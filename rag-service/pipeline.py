import json
import re
from operator import itemgetter
from urllib.parse import quote

import requests
import torch
from scipy import spatial
from sentence_transformers import SentenceTransformer

from outils_systeme import TOOLS_SYSTEM, TOOLS_DESCRIPTION

LLM_URL = "http://localhost:8080/v1/chat/completions"
HEADERS = {"User-Agent": "rag-exercice/1.0"}
MAX_STEPS = 8

embedder = SentenceTransformer("Qwen/Qwen3-Embedding-0.6B", device="cuda" if torch.cuda.is_available() else "cpu")
embedder.max_seq_length = 256

SYSTEM = (
    "Tu es un assistant qui répond en français. "
    "Tu réponds toujours par un unique objet JSON, sans aucun autre texte. "
    "Deux formats sont possibles. "
    "Pour appeler un outil : {\"tool\": \"nom_outil\", \"arguments\": {\"param\": \"valeur\"}}. "
    "Pour donner la réponse finale : {\"answer\": \"ta réponse en une ou deux phrases\"}. "
    "Outils disponibles : "
    "get_weather avec l'argument city (météo actuelle d'une ville), "
    "wikipedia_summary avec l'argument title (résumé d'un article Wikipédia). "
    "Utilise d'abord le contexte fourni. "
    "N'appelle un outil que si le contexte ne suffit pas ou si la question porte sur la météo actuelle. "
    "Quand tu reçois le résultat d'un outil, utilise-le pour donner la réponse finale."
)
SYSTEM = SYSTEM + " " + TOOLS_DESCRIPTION
SYSTEM = SYSTEM + " Tu peux enchaîner plusieurs outils l'un après l'autre, avec un seul appel par réponse. Quand une tâche demande plusieurs étapes, fais-les toutes avant de donner la réponse finale."


def chunk_text(text, size=120, overlap=20):
    words = text.split()
    step = size - overlap
    chunks = []
    for start in range(0, len(words), step):
        piece = " ".join(words[start:start + size])
        if piece:
            chunks.append(piece)
    return chunks


with open("corpus.txt", encoding="utf-8") as f:
    chunks = chunk_text(f.read())

encoded_chunks = embedder.encode(chunks, show_progress_bar=True, batch_size=32)
print("nombre de chunks :", len(chunks))


def top_chunks(question, k=5):
    question_vector = embedder.encode(question, prompt_name="query")
    cosine_list = []
    for index_chunk, vector in enumerate(encoded_chunks):
        result = 1 - spatial.distance.cosine(question_vector, vector)
        cosine_list.append({"res": result, "i": index_chunk})
    cosine_list.sort(key=itemgetter("res"), reverse=True)
    return cosine_list[:k]


def get_weather(city):
    geo = requests.get(
        "https://geocoding-api.open-meteo.com/v1/search",
        params={"name": city, "count": 1, "language": "fr"},
        headers=HEADERS,
        timeout=10,
    ).json()
    if "results" not in geo:
        return "Ville introuvable"
    place = geo["results"][0]
    meteo = requests.get(
        "https://api.open-meteo.com/v1/forecast",
        params={
            "latitude": place["latitude"],
            "longitude": place["longitude"],
            "current_weather": True,
        },
        headers=HEADERS,
        timeout=10,
    ).json()
    current = meteo["current_weather"]
    return (
        place["name"]
        + " : "
        + str(current["temperature"])
        + " °C, vent "
        + str(current["windspeed"])
        + " km/h"
    )


def wikipedia_summary(title):
    url = "https://fr.wikipedia.org/api/rest_v1/page/summary/" + quote(title.replace(" ", "_"))
    response = requests.get(url, headers=HEADERS, timeout=10)
    if response.status_code != 200:
        return "Article introuvable"
    return response.json().get("extract", "Aucun résumé disponible")


TOOLS = {"get_weather": get_weather, "wikipedia_summary": wikipedia_summary}
TOOLS.update(TOOLS_SYSTEM)


def run_tool(name, arguments):
    if name not in TOOLS:
        return "Outil inconnu : " + str(name)
    try:
        return str(TOOLS[name](**arguments))
    except Exception as error:
        return "Erreur : " + str(error)


def ask_llm(messages):
    response = requests.post(
        LLM_URL,
        json={
            "messages": messages,
            "temperature": 0,
            "max_tokens": 400,
            "response_format": {"type": "json_object"},
            "chat_template_kwargs": {"enable_thinking": False},
        },
        timeout=300,
    )
    return response.json()["choices"][0]["message"]["content"]


def parse_json(text):
    text = re.sub(r"<think>.*?</think>", "", text, flags=re.DOTALL)
    match = re.search(r"\{.*\}", text, flags=re.DOTALL)
    return json.loads(match.group(0))


def run(question):
    selected = top_chunks(question)
    yield "passages", ", ".join(
        "n°" + str(item["i"]) + " (" + str(round(item["res"], 3)) + ")" for item in selected
    )
    context = "\n\n".join(chunks[item["i"]] for item in selected)
    messages = [
        {"role": "system", "content": SYSTEM},
        {"role": "user", "content": "Contexte :\n" + context + "\n\nQuestion : " + question},
    ]
    for _ in range(MAX_STEPS):
        raw = ask_llm(messages)
        try:
            data = parse_json(raw)
        except Exception:
            messages.append({"role": "assistant", "content": raw})
            messages.append({"role": "user", "content": "Format invalide, renvoie uniquement un objet JSON valide."})
            yield "warning", "Réponse du modèle invalide, nouvelle tentative"
            continue
        if "tool" in data:
            name = data["tool"]
            arguments = data.get("arguments", {})
            yield "tool", str(name) + " " + json.dumps(arguments, ensure_ascii=False)
            result = run_tool(name, arguments)
            yield "result", result[:200]
            messages.append({"role": "assistant", "content": raw})
            messages.append({"role": "user", "content": "Résultat de l'outil : " + result})
        else:
            yield "final", str(data.get("answer", raw))
            return
    yield "final", "Nombre maximal d'étapes atteint"