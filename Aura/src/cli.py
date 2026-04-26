import sys

from src.agent import AuraAgent
from src.config import load_config, save_config


def _show_config() -> None:
    cfg = load_config()
    print(f"Config file: ~/.aura/config.json\n")
    print(f"  asis_path: {cfg.asis_path}")
    print(f"  doc_path:  {cfg.doc_path}")
    print(f"  api_key:   {'*' * 8 + cfg.llm.api_key[-4:] if cfg.llm.api_key else '(not set)'}")
    print(f"  base_url:  {cfg.llm.base_url}")
    print(f"  model:     {cfg.llm.model}")


def _set_config(args: list[str]) -> None:
    cfg = load_config()
    if not args:
        print("Usage: aura config set <key> <value>")
        print("Keys: api_key, base_url, model, asis_path, doc_path")
        return

    key, *rest = args
    value = " ".join(rest)
    key_map = {
        "api_key": lambda c, v: setattr(c.llm, "api_key", v),
        "base_url": lambda c, v: setattr(c.llm, "base_url", v),
        "model": lambda c, v: setattr(c.llm, "model", v),
        "asis_path": lambda c, v: setattr(c, "asis_path", v),
        "doc_path": lambda c, v: setattr(c, "doc_path", v),
    }

    if key not in key_map:
        print(f"Unknown key: {key}")
        print(f"Valid keys: {', '.join(key_map)}")
        return

    key_map[key](cfg, value)
    save_config(cfg)
    print(f"Set {key} = {value if key != 'api_key' else '*' * 8 + value[-4:]}")


def _config_command(args: list[str]) -> None:
    if not args or args[0] == "show":
        _show_config()
    elif args[0] == "set":
        _set_config(args[1:])
    else:
        print("Usage: aura config [show|set]")


def main() -> None:
    args = sys.argv[1:]

    if args and args[0] == "config":
        _config_command(args[1:])
        return

    config = load_config()
    config.resolve_paths(sys.path[0] if sys.path[0] else ".")

    if not config.llm.api_key:
        print("Error: API key not set. Run 'aura config set api_key <key>' to configure.")
        sys.exit(1)

    if not config.asis_path.exists():
        print(f"Error: ASIS executable not found at {config.asis_path}")
        print("Set asis_path in the config file (~/.aura/config.json).")
        sys.exit(1)

    agent = AuraAgent(config)
    agent.start()

    print("Aura agent ready. Type 'exit' to quit.\n")
    try:
        while True:
            try:
                user_input = input("You: ").strip()
            except EOFError:
                break

            if not user_input:
                continue
            if user_input.lower() == "exit":
                break

            response = agent.chat(user_input)
            print(f"\nAura: {response}\n")
    except KeyboardInterrupt:
        print()
    finally:
        agent.close()
