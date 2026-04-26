import json
from pathlib import Path
from typing import Any

from openai import OpenAI

from src.asis_client import ASISClient
from src.config import AppConfig


class AuraAgent:
    SYSTEM_PROMPT = (
        "You are Aura, an intelligent AI agent that helps users manage file archives "
        "through the ASIS CLI.\n\n"
        "Your capabilities:\n"
        "- Create and open archives\n"
        "- Import files with tags and descriptions\n"
        "- Search files by name, tags, or date range\n"
        "- Rename, retag, describe, and delete files\n"
        "- Perform archive maintenance (diff, unlink)\n\n"
        "Guidelines:\n"
        "- Always confirm destructive actions (delete, unlink) with the user before executing\n"
        "- When a user mentions files or archives, use the ASIS CLI to help them\n"
        "- If multiple files match a name, ask the user to be more specific or use id:<guid>\n"
        "- Present search results in a clean, readable format\n"
        "- If ASIS returns an error, explain it clearly and suggest fixes\n\n"
        "The ASIS.CLI is an interactive shell. You send commands and receive text output.\n"
        "Available commands: create, open, close, archive, diff, import, rename, retag, "
        "tag, info, describe, delete, unlink, search, id, help, exit.\n\n"
        "You have access to full ASIS documentation below. Use it to answer questions "
        "and construct correct commands."
    )

    _TOOLS: list[dict[str, Any]] = [
        {
            "type": "function",
            "function": {
                "name": "run_asis_command",
                "description": (
                    "Run a single command in the ASIS.CLI interactive shell. "
                    "The command will be executed and the text output returned."
                ),
                "parameters": {
                    "type": "object",
                    "properties": {
                        "command": {
                            "type": "string",
                            "description": (
                                "The ASIS CLI command to execute, e.g. 'open ./archives/projects' "
                                "or 'search tag documentation'"
                            ),
                        }
                    },
                    "required": ["command"],
                },
            },
        }
    ]

    def __init__(self, config: AppConfig):
        self.config = config
        self.client = OpenAI(
            api_key=config.llm.api_key,
            base_url=config.llm.base_url,
        )
        self.asis = ASISClient(config.asis_path)
        self.history: list[dict[str, Any]] = []
        self.docs = self._load_docs()

    def _load_docs(self) -> str:
        parts: list[str] = []
        p = self.config.doc_path
        if p.exists():
            if p.is_file():
                parts.append(f"--- {p.name} ---\n{p.read_text(encoding='utf-8')}")
            else:
                for path in sorted(p.glob("*.md")):
                    parts.append(f"--- {path.name} ---\n{path.read_text(encoding='utf-8')}")
        return "\n\n".join(parts)

    def start(self) -> None:
        self.asis.start()

    def close(self) -> None:
        self.asis.close()

    def _build_messages(self) -> list[dict[str, Any]]:
        system_msg = {
            "role": "system",
            "content": f"{self.SYSTEM_PROMPT}\n\nDocumentation:\n{self.docs}",
        }
        return [system_msg] + self.history

    def chat(self, user_input: str) -> str:
        self.history.append({"role": "user", "content": user_input})

        while True:
            response = self.client.chat.completions.create(
                model=self.config.llm.model,
                max_tokens=4096,
                messages=self._build_messages(),
                tools=self._TOOLS,
            )

            message = response.choices[0].message
            assistant_msg: dict[str, Any] = {
                "role": "assistant",
                "content": message.content or "",
            }

            if message.tool_calls:
                assistant_msg["tool_calls"] = [
                    {
                        "id": tc.id,
                        "type": "function",
                        "function": {
                            "name": tc.function.name,
                            "arguments": tc.function.arguments,
                        },
                    }
                    for tc in message.tool_calls
                ]
                self.history.append(assistant_msg)

                tool_results: list[dict[str, Any]] = []
                for tc in message.tool_calls:
                    if tc.function.name == "run_asis_command":
                        args = json.loads(tc.function.arguments)
                        command = args.get("command", "")
                        result = self.asis.send(command)
                        tool_results.append(
                            {
                                "role": "tool",
                                "tool_call_id": tc.id,
                                "content": result,
                            }
                        )
                self.history.extend(tool_results)
                continue

            self.history.append(assistant_msg)
            return message.content or ""
