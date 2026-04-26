import json
from pathlib import Path
from typing import Any

from pydantic import BaseModel

CONFIG_DIR = Path.home() / ".aura"
CONFIG_FILE = CONFIG_DIR / "config.json"


class LLMConfig(BaseModel):
    api_key: str = ""
    base_url: str = "https://api.openai.com/v1"
    model: str = "gpt-4o"


class AppConfig(BaseModel):
    asis_path: Path = Path("../Tools/ASIS/ASIS.CLI/bin/Release/net10.0/asis.exe")
    doc_path: Path = Path("../doc/asis/asis_cli_usage.md")
    llm: LLMConfig = LLMConfig()

    def resolve_paths(self, base: Path) -> None:
        if not self.asis_path.is_absolute():
            self.asis_path = (base / self.asis_path).resolve()
        if not self.doc_path.is_absolute():
            self.doc_path = (base / self.doc_path).resolve()


def load_config() -> AppConfig:
    if CONFIG_FILE.exists():
        try:
            data: dict[str, Any] = json.loads(CONFIG_FILE.read_text(encoding="utf-8"))
            return AppConfig(**data)
        except Exception:
            pass
    return AppConfig()


def save_config(cfg: AppConfig) -> None:
    CONFIG_DIR.mkdir(parents=True, exist_ok=True)
    CONFIG_FILE.write_text(
        cfg.model_dump_json(indent=2) + "\n", encoding="utf-8"
    )
