import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

from openai import OpenAI
from rich.console import Console
from rich.panel import Panel

from src.asis_client import ASISClient
from src.config import AppConfig

MAX_TOOL_ITERATIONS = 10


@dataclass
class TokenUsage:
    prompt_tokens: int = 0
    completion_tokens: int = 0
    total_tokens: int = 0


class AuraAgent:
    SYSTEM_PROMPT = (
        "You are Aura, a helpful and capable AI assistant.\n\n"
        "## Core Identity\n"
        "You are a general-purpose daily assistant. You help with writing, analysis, "
        "coding, research, planning, conversation, and any other task the user brings "
        "to you.\n\n"
        "## Archive Management Tool\n"
        "You also have access to ASIS, a file archive management system. When the user "
        "needs to manage file archives \u2014 creating, opening, importing, searching, "
        "organizing, or maintaining archives \u2014 use the `run_asis_command` tool to "
        "interact with ASIS.CLI.\n\n"
        "### ASIS Commands Reference\n"
        "Available commands: create, open, close, archive, diff, import, rename, retag, "
        "tag, info, describe, delete, unlink, search, id, help.\n\n"
        "### Guidelines for ASIS\n"
        "- Confirm destructive actions (delete, unlink) before executing\n"
        "- When multiple files match, ask the user to be specific or use id:<guid>\n"
        "- Present search results cleanly\n"
        "- If ASIS returns an error, explain it and suggest fixes\n\n"
        "## General Guidelines\n"
        "- Be concise, helpful, and accurate\n"
        "- When unsure, say so rather than guessing\n"
        "- Format responses with markdown when it improves readability"
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
                                "The ASIS CLI command to execute, e.g. "
                                "'open ./archives/projects' or 'search tag documentation'"
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

    def chat_stream(self, user_input: str, console: Console) -> TokenUsage:
        self.history.append({"role": "user", "content": user_input})
        usage = TokenUsage()

        for iteration in range(MAX_TOOL_ITERATIONS):
            stream = self.client.chat.completions.create(
                model=self.config.llm.model_name,
                max_tokens=4096,
                messages=self._build_messages(),
                tools=self._TOOLS,
                stream=True,
                stream_options={"include_usage": True},
            )

            content_parts: list[str] = []
            tool_calls: dict[int, dict[str, Any]] = {}

            for chunk in stream:
                if not chunk.choices:
                    if chunk.usage:
                        usage.prompt_tokens = chunk.usage.prompt_tokens
                        usage.completion_tokens = chunk.usage.completion_tokens
                        usage.total_tokens = chunk.usage.total_tokens
                    continue

                delta = chunk.choices[0].delta

                if delta.content:
                    content_parts.append(delta.content)
                    console.print(delta.content, end="", highlight=False)

                if delta.tool_calls:
                    for tc_delta in delta.tool_calls:
                        idx = tc_delta.index
                        if idx not in tool_calls:
                            tool_calls[idx] = {"id": "", "name": "", "arguments": ""}
                        if tc_delta.id:
                            tool_calls[idx]["id"] = tc_delta.id
                        if tc_delta.function:
                            if tc_delta.function.name:
                                tool_calls[idx]["name"] = tc_delta.function.name
                            if tc_delta.function.arguments:
                                tool_calls[idx]["arguments"] += tc_delta.function.arguments

            if content_parts:
                console.print()

            full_content = "".join(content_parts)
            assistant_msg: dict[str, Any] = {
                "role": "assistant",
                "content": full_content,
            }

            if tool_calls:
                assistant_msg["tool_calls"] = [
                    {
                        "id": tc["id"],
                        "type": "function",
                        "function": {
                            "name": tc["name"],
                            "arguments": tc["arguments"],
                        },
                    }
                    for tc in tool_calls.values()
                ]
                self.history.append(assistant_msg)

                for tc in tool_calls.values():
                    name = tc["name"]
                    args = json.loads(tc["arguments"])
                    if name == "run_asis_command":
                        command = args.get("command", "")
                        with console.status(f"[dim]Running ASIS: {command}[/dim]"):
                            result = self.asis.send(command)
                        console.print(
                            Panel(
                                result,
                                title=f"ASIS: {command}",
                                border_style="dim",
                            )
                        )
                        self.history.append({
                            "role": "tool",
                            "tool_call_id": tc["id"],
                            "content": result,
                        })
                continue

            self.history.append(assistant_msg)
            return usage

        console.print("\n[yellow]Warning: reached maximum tool iterations.[/yellow]")
        return usage
