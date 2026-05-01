import sys
from pathlib import Path

from rich.console import Console
from rich.panel import Panel

from src.agent import AuraAgent
from src.config import load_config, save_config

console = Console()


def _show_config() -> None:
    cfg = load_config()
    console.print(f"Config file: ~/.aura/config.json\n")
    console.print(f"  asis_path:   {cfg.asis_path}")
    console.print(f"  doc_path:    {cfg.doc_path}")
    api_key_display = (
        "*" * 8 + cfg.llm.api_key[-4:] if cfg.llm.api_key else "(not set)"
    )
    console.print(f"  api_key:     {api_key_display}")
    console.print(f"  base_url:    {cfg.llm.base_url}")
    console.print(f"  model_name:  {cfg.llm.model_name}")


def _set_config(args: list[str]) -> None:
    cfg = load_config()
    if not args:
        console.print("Usage: aura config set <key> <value>")
        console.print("Keys: api_key, base_url, model_name, asis_path, doc_path")
        return

    key, *rest = args
    value = " ".join(rest)
    key_map = {
        "api_key": lambda c, v: setattr(c.llm, "api_key", v),
        "base_url": lambda c, v: setattr(c.llm, "base_url", v),
        "model_name": lambda c, v: setattr(c.llm, "model_name", v),
        "model": lambda c, v: setattr(c.llm, "model_name", v),
        "asis_path": lambda c, v: setattr(c, "asis_path", v),
        "doc_path": lambda c, v: setattr(c, "doc_path", v),
    }

    if key not in key_map:
        console.print(f"[red]Unknown key:[/] {key}")
        console.print(f"Valid keys: {', '.join(key_map)}")
        return

    key_map[key](cfg, value)
    save_config(cfg)
    display_value = value if key != "api_key" else "*" * 8 + value[-4:]
    console.print(f"[green]Set[/] {key} = {display_value}")


def _config_command(args: list[str]) -> None:
    if not args or args[0] == "show":
        _show_config()
    elif args[0] == "set":
        _set_config(args[1:])
    else:
        console.print("Usage: aura config [show|set]")


def main() -> None:
    args = sys.argv[1:]

    if args and args[0] == "config":
        _config_command(args[1:])
        return

    config = load_config()
    config.resolve_paths(sys.path[0] if sys.path[0] else ".")

    if not config.llm.api_key:
        console.print(
            "[red]Error:[/] API key not set. "
            "Run [bold]aura config set api_key <key>[/bold] to configure."
        )
        sys.exit(1)

    if not config.asis_path.exists():
        console.print(f"[red]Error:[/] ASIS executable not found at {config.asis_path}")
        console.print("Set asis_path in the config file (~/.aura/config.json).")
        sys.exit(1)

    agent = AuraAgent(config)
    agent.start()

    console.print(
        Panel("[bold]Aura[/bold] \u2014 Your AI assistant", border_style="cyan")
    )
    console.print("[dim]Type 'exit' to quit.[/dim]\n")

    model_name = config.llm.model_name
    input_prompt = f"[bold green]You[/] [dim]({model_name})[/]: "

    try:
        while True:
            try:
                user_input = console.input(input_prompt).strip()
            except EOFError:
                break

            if not user_input:
                continue
            if user_input.lower() == "exit":
                break

            console.print("[bold cyan]Aura:[/] ", end="")
            usage = agent.chat_stream(user_input, console)
            console.print(
                f"\n[dim]Tokens \u2014 prompt: {usage.prompt_tokens}, "
                f"completion: {usage.completion_tokens}, "
                f"total: {usage.total_tokens}[/dim]\n"
            )
    except KeyboardInterrupt:
        console.print("\n[dim]Interrupted.[/dim]")
    finally:
        agent.close()
