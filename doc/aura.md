# aura

AI agent for managing ASIS archives through natural language.

## Setup

1. Copy `.env.example` to `.env` and set your Anthropic API key.
2. Adjust `AURA_ASIS_PATH` and `AURA_DOC_PATH` if needed.
3. Run `uv sync` to create the virtual environment and install dependencies.

## Usage

    uv run aura

Or after activating the virtual environment:

    python -m aura

## REPL Commands

| Command | Description |
|---------|-------------|
| `/exit` | Exit the program |

All other input, including the word `exit`, is treated as normal conversation and sent to the LLM.

## Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `AURA_ANTHROPIC_API_KEY` | Anthropic API key | (required) |
| `AURA_ASIS_PATH` | Path to `asis.exe` | `../Tools/ASIS/ASIS.CLI/bin/Release/net10.0/asis.exe` |
| `AURA_DOC_PATH` | Path to ASIS docs | `../doc/asis` |
| `AURA_MODEL` | Claude model | `claude-sonnet-4-6` |
