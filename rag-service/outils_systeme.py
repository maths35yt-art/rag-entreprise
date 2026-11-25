import os
import shlex
import subprocess

WORKSPACE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "workspace")
os.makedirs(WORKSPACE, exist_ok=True)

ALLOWED_COMMANDS = {"python", "hostname", "whoami", "ipconfig"}

TOOLS_DESCRIPTION = (
    "Outils supplémentaires sur le dossier de travail : "
    "list_files sans argument (liste les fichiers), "
    "read_file avec l'argument filename (lit un fichier), "
    "write_file avec les arguments filename et content (écrit un fichier), "
    "run_command avec l'argument command (exécute une commande parmi : python, hostname, whoami, ipconfig). "
    "Les écritures et les commandes demandent la validation de l'utilisateur."
)

CONFIRM_HANDLER = None


def set_confirm_handler(handler):
    global CONFIRM_HANDLER
    CONFIRM_HANDLER = handler


def safe_path(filename):
    path = os.path.abspath(os.path.join(WORKSPACE, filename))
    if os.path.commonpath([path, WORKSPACE]) != WORKSPACE:
        raise ValueError("Chemin hors du dossier de travail")
    return path


def confirm(action):
    if CONFIRM_HANDLER is not None:
        return CONFIRM_HANDLER(action)
    return input("Autoriser : " + action + " ? (o/n) ").strip().lower() == "o"


def list_files():
    return ", ".join(os.listdir(WORKSPACE)) or "Dossier vide"


def read_file(filename):
    with open(safe_path(filename), encoding="utf-8") as f:
        return f.read()[:3000]


def write_file(filename, content):
    path = safe_path(filename)
    if not confirm("écrire le fichier " + filename):
        return "Refusé par l'utilisateur"
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
    return "Fichier écrit : " + filename


def run_command(command):
    parts = shlex.split(command, posix=False)
    if not parts or parts[0] not in ALLOWED_COMMANDS:
        return "Commande non autorisée"
    if not confirm("exécuter " + command):
        return "Refusé par l'utilisateur"
    result = subprocess.run(parts, cwd=WORKSPACE, capture_output=True, text=True, timeout=20)
    return (result.stdout + result.stderr)[:2000]


TOOLS_SYSTEM = {
    "list_files": list_files,
    "read_file": read_file,
    "write_file": write_file,
    "run_command": run_command,
}