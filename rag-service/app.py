import os
import re
import threading

import requests
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

import pipeline
from outils_systeme import set_confirm_handler
from pipeline import chunks, run

pipeline.LLM_URL = os.environ.get("LLM_URL", pipeline.LLM_URL)

PASSAGE_PATTERN = re.compile(r"n°(\d+) \(([-0-9.]+)\)")


def deny(action):
    return False


set_confirm_handler(deny)


class AskRequest(BaseModel):
    question: str = Field(min_length=1, max_length=2000)


class Passage(BaseModel):
    index: int
    score: float
    text: str


class Step(BaseModel):
    kind: str
    text: str


class AskResponse(BaseModel):
    answer: str
    passages: list[Passage]
    steps: list[Step]


app = FastAPI(title="Service RAG", version="1.0.0")
lock = threading.Lock()


@app.get("/health")
def health():
    return {"status": "ok", "chunks": len(chunks)}


@app.post("/ask", response_model=AskResponse)
def ask(request: AskRequest):
    passages = []
    steps = []
    answer = ""
    try:
        with lock:
            for kind, text in run(request.question):
                if kind == "passages":
                    for index, score in PASSAGE_PATTERN.findall(text):
                        passages.append(Passage(index=int(index), score=float(score), text=chunks[int(index)]))
                elif kind == "final":
                    answer = text
                else:
                    steps.append(Step(kind=kind, text=text))
    except requests.exceptions.RequestException:
        raise HTTPException(status_code=503, detail="Serveur LLM injoignable")
    return AskResponse(answer=answer, passages=passages, steps=steps)