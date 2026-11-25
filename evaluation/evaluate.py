import argparse
import json
import re
import statistics
import sys
import time
import unicodedata
from datetime import datetime
from pathlib import Path

import requests

HERE = Path(__file__).resolve().parent


def normalize(text):
    text = text.lower().replace("đ", "d").replace("œ", "oe")
    text = unicodedata.normalize("NFKD", text)
    text = "".join(char for char in text if not unicodedata.combining(char))
    text = re.sub(r"[^a-z0-9]+", " ", text)
    return " " + text.strip() + " "


def matches(text, groups):
    haystack = normalize(text)
    return all(any(normalize(variant) in haystack for variant in group) for group in groups)


def ask(url, question, timeout):
    started = time.perf_counter()
    response = requests.post(url, json={"question": question}, timeout=timeout)
    seconds = time.perf_counter() - started
    response.raise_for_status()
    return response.json(), seconds


def evaluate_one(item, url, timeout):
    row = {
        "id": item["id"],
        "question": item["question"],
        "expected": item["expected"],
        "answer": "",
        "passages": [],
        "tools": 0,
        "retrieved": False,
        "correct": False,
        "cause": "",
        "seconds": 0.0,
    }
    try:
        data, seconds = ask(url, item["question"], timeout)
    except (requests.RequestException, ValueError) as error:
        row["cause"] = "service"
        row["answer"] = "Erreur : " + str(error)
        return row
    answer = data.get("answer", "")
    passages = data.get("passages", [])
    passages_text = " ".join(passage["text"] for passage in passages)
    row["answer"] = answer
    row["passages"] = [passage["index"] for passage in passages]
    row["tools"] = sum(1 for step in data.get("steps", []) if step.get("kind") == "tool")
    row["seconds"] = round(seconds, 1)
    row["retrieved"] = matches(passages_text, item["keywords"])
    row["correct"] = matches(answer, item["keywords"])
    if not row["correct"]:
        row["cause"] = "génération" if row["retrieved"] else "récupération"
    return row


def cell(value):
    return str(value).replace("|", "/").replace("\r", " ").replace("\n", " ").strip()


def build_report(rows, url):
    total = len(rows)
    correct = sum(1 for row in rows if row["correct"])
    served = [row for row in rows if row["cause"] != "service"]
    retrieved = sum(1 for row in served if row["retrieved"])
    with_tool = sum(1 for row in served if row["tools"] > 0)
    latency = statistics.mean(row["seconds"] for row in served) if served else 0.0
    causes = {
        name: sum(1 for row in rows if row["cause"] == name)
        for name in ("récupération", "génération", "service")
    }
    lines = [
        "# Évaluation du RAG",
        "",
        "Date : " + datetime.now().strftime("%d/%m/%Y %H:%M") + ". Service évalué : " + url + ".",
        "",
        "Taux de réponses correctes : " + str(correct) + " sur " + str(total)
        + " (" + str(round(100 * correct / total)) + " %).",
        "",
        "Passage utile présent parmi les passages récupérés : " + str(retrieved) + " sur " + str(len(served))
        + ". Questions ayant déclenché un outil externe : " + str(with_tool) + " sur " + str(len(served))
        + ". Temps de réponse moyen : " + str(round(latency, 1)) + " s.",
        "",
        "Erreurs par cause : récupération " + str(causes["récupération"])
        + ", génération " + str(causes["génération"])
        + ", service " + str(causes["service"]) + ".",
        "",
        "## Tableau des erreurs",
        "",
    ]
    errors = [row for row in rows if not row["correct"]]
    if errors:
        lines.append("| N° | Question | Réponse attendue | Réponse obtenue | Passage utile récupéré | Cause |")
        lines.append("|---|---|---|---|---|---|")
        for row in errors:
            lines.append(
                "| "
                + " | ".join(
                    [
                        cell(row["id"]),
                        cell(row["question"]),
                        cell(row["expected"]),
                        cell(row["answer"]),
                        "oui" if row["retrieved"] else "non",
                        cell(row["cause"]),
                    ]
                )
                + " |"
            )
    else:
        lines.append("Aucune erreur sur ce jeu de questions.")
    lines += [
        "",
        "## Méthode",
        "",
        "Une réponse est jugée correcte si elle contient tous les éléments attendus, la comparaison ignorant la casse, "
        "les accents et la ponctuation. Cette méthode peut rejeter une bonne réponse formulée avec d'autres mots, "
        "c'est pourquoi la réponse obtenue figure dans le tableau pour relecture. Une erreur est attribuée à la "
        "récupération quand les éléments attendus sont absents des passages fournis au modèle, à la génération quand "
        "ils étaient présents mais n'apparaissent pas dans la réponse, et au service quand la requête a échoué.",
    ]
    return "\n".join(lines) + "\n"


def main():
    sys.stdout.reconfigure(errors="replace")
    parser = argparse.ArgumentParser(description="Évaluation du service RAG")
    parser.add_argument("--url", default="http://localhost:8000/ask")
    parser.add_argument("--questions", default=str(HERE / "questions.json"))
    parser.add_argument("--output", default=str(HERE))
    parser.add_argument("--timeout", type=int, default=300)
    parser.add_argument("--min-accuracy", type=float, default=0.0)
    args = parser.parse_args()

    questions = json.loads(Path(args.questions).read_text(encoding="utf-8"))
    if not questions:
        raise SystemExit("Aucune question à évaluer")

    rows = []
    for position, item in enumerate(questions, start=1):
        row = evaluate_one(item, args.url, args.timeout)
        rows.append(row)
        status = "OK" if row["correct"] else "ERREUR (" + row["cause"] + ")"
        print(
            "[" + str(position) + "/" + str(len(questions)) + "] " + status + " "
            + str(row["seconds"]) + " s : " + item["question"],
            flush=True,
        )

    output = Path(args.output)
    output.mkdir(parents=True, exist_ok=True)
    (output / "results.json").write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding="utf-8")
    (output / "report.md").write_text(build_report(rows, args.url), encoding="utf-8")

    accuracy = sum(1 for row in rows if row["correct"]) / len(rows)
    print("Taux de réponses correctes : " + str(round(100 * accuracy)) + " %")
    print("Rapport écrit dans " + str(output / "report.md"))
    if accuracy < args.min_accuracy:
        sys.exit(1)


if __name__ == "__main__":
    main()