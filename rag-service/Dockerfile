FROM python:3.12-slim

ENV PYTHONUNBUFFERED=1 \
    HF_HOME=/models/hf

WORKDIR /app

RUN pip install --no-cache-dir torch --index-url https://download.pytorch.org/whl/cpu

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

RUN useradd --create-home app \
    && mkdir -p /models/hf /app/workspace \
    && chown -R app:app /models /app

COPY --chown=app:app app.py pipeline.py outils_systeme.py corpus.txt ./

USER app

EXPOSE 8000

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8000"]