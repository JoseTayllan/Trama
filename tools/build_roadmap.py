"""Generate the offline roadmap dashboard from rodmap.md; no third-party packages."""
from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[1]


def section(text, heading):
    match = re.search(r"^#{2,3} " + re.escape(heading) + r"\s*\n(.*?)(?=^#{2,3} |\Z)", text, re.M | re.S)
    return match.group(1) if match else ""


def table(text):
    rows = [line.strip().strip("|").split("|") for line in text.splitlines() if line.startswith("|")]
    return [[cell.strip() for cell in row] for row in rows[2:]]


def checklist(text):
    return [{"done": mark.lower() == "x", "text": label} for mark, label in re.findall(r"^- \[([ xX])\] (.+)$", text, re.M)]


def build():
    text = (ROOT / "rodmap.md").read_text(encoding="utf-8-sig")
    now = dict(table(section(text, "Agora")))
    focus_heading = re.search(r"^### (Entrega em foco: .+)$", text, re.M)
    focus = focus_heading.group(1) if focus_heading else "Entrega em foco: exportação"

    def document_link(value, fallback):
        match = re.fullmatch(r"\[([^\]]+)\]\((docs/[A-Za-z0-9_./-]+\.md)\)", value)
        if match and ".." not in match.group(2):
            return {"label": match.group(1), "href": match.group(2)}
        return {"label": "Central de estudos", "href": fallback}

    data = {
        "date": re.search(r"Atualizado em \*\*(.+?)\*\*", text).group(1),
        "now": now,
        "module": now.get("Foco", "Projeto Trama"),
        "reference": document_link(now.get("Referência principal", ""), "docs/estudos/README.md"),
        "review": document_link(now.get("Relatório atual", ""), "docs/reviews/055-markdown-export-green.md"),
        "stages": table(section(text, "Etapas do módulo atual") or section(text, "Caminho para fechar os cadernos")),
        "available": table(section(text, "O que já está disponível")),
        "future": table(section(text, "Evolução futura")),
        "technical": table(section(text, "Pendências técnicas")),
        "tasks": checklist(section(text, focus)),
        "acceptance": checklist(section(text, "Critério de conclusão desta versão")),
    }
    template = (ROOT / "tools" / "roadmap.template.html").read_text(encoding="utf-8")
    payload = json.dumps(data, ensure_ascii=False).replace("<", "\\u003c").replace("&", "\\u0026")
    (ROOT / "roadmap.html").write_text(template.replace("__ROADMAP_DATA__", payload), encoding="utf-8")
    print("roadmap.html atualizado a partir de rodmap.md")


if __name__ == "__main__":
    build()
