#!/usr/bin/env python3
"""Generate an HTML QR-code sheet for GitHub and external links."""

from __future__ import annotations

import argparse
import html
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from urllib.parse import quote, urlparse


DEFAULT_BRANCH = "main"
DEFAULT_QR_SIZE = 1024
DEFAULT_DISPLAY_SIZE = 220
DEFAULT_ACCENT_COLOR = "#000000"
DEFAULT_QR_STYLE = {
    "dots": "square",
    "cornersSquare": "square",
    "cornersDot": "square",
}
QR_DOT_STYLES = {"rounded", "dots", "classy", "classy-rounded", "square", "extra-rounded"}
QR_CORNER_SQUARE_STYLES = {"dot", "square", "extra-rounded", "rounded", "dots", "classy", "classy-rounded"}
QR_CORNER_DOT_STYLES = {"dot", "square", "rounded", "dots", "classy", "classy-rounded", "extra-rounded"}


@dataclass(frozen=True)
class FileLink:
    label: str
    url: str
    kind: str


@dataclass(frozen=True)
class Config:
    paths: list[str]
    external_links: list[FileLink]
    include_repo_root: bool
    repo_root_label: str
    accent_color: str | None
    qr_style: dict[str, str]


def run_git(args: list[str], cwd: Path) -> str | None:
    try:
        result = subprocess.run(
            ["git", *args],
            cwd=cwd,
            check=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            text=True,
        )
    except (FileNotFoundError, subprocess.CalledProcessError):
        return None

    value = result.stdout.strip()
    return value or None


def find_repo_root(cwd: Path) -> Path:
    root = run_git(["rev-parse", "--show-toplevel"], cwd)
    if root is None:
        raise SystemExit("error: this script must run inside a git repository")

    return Path(root).resolve()


def github_repo_url(explicit_repo_url: str | None, repo_root: Path) -> str:
    if explicit_repo_url:
        return normalize_github_remote(explicit_repo_url)

    github_repository = os.environ.get("GITHUB_REPOSITORY")
    github_server_url = os.environ.get("GITHUB_SERVER_URL", "https://github.com")
    if github_repository:
        return f"{github_server_url.rstrip('/')}/{github_repository}".removesuffix(".git")

    remote_url = run_git(["remote", "get-url", "origin"], repo_root)
    if remote_url is None:
        raise SystemExit("error: pass --repo-url or configure a git remote named origin")

    return normalize_github_remote(remote_url)


def normalize_github_remote(remote_url: str) -> str:
    remote_url = remote_url.strip()

    ssh_like = re.fullmatch(r"ssh://(?:[^@]+@)?([^/]+)/(.+)", remote_url)
    if ssh_like:
        host, path = ssh_like.groups()
        return f"https://{host}/{path.removesuffix('.git')}"

    scp_like = re.fullmatch(r"(?:[^@]+@)?([^:]+):(.+)", remote_url)
    if scp_like:
        host, path = scp_like.groups()
        return f"https://{host}/{path.removesuffix('.git')}"

    https_like = re.fullmatch(r"https?://([^/]+)/(.+)", remote_url)
    if https_like:
        host, path = https_like.groups()
        return f"https://{host}/{path.removesuffix('.git')}"

    raise SystemExit(f"error: unsupported GitHub remote URL: {remote_url}")


def read_config(config_file: Path) -> Config:
    if not config_file.exists():
        raise SystemExit(f"error: config file does not exist: {config_file}")

    try:
        config = json.loads(config_file.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise SystemExit(f"error: invalid JSON in {config_file}: {exc}") from exc

    paths = config.get("paths", [])
    external_links = config.get("externalLinks", [])
    include_repo_root = config.get("includeRepoRoot", False)
    repo_root_label = config.get("repoRootLabel", "GitHub repository")
    accent_color = config.get("accentColor")
    qr_style = config.get("qrStyle", {})

    if not isinstance(paths, list) or not all(isinstance(path, str) for path in paths):
        raise SystemExit('error: config field "paths" must be an array of strings')
    if not isinstance(external_links, list):
        raise SystemExit('error: config field "externalLinks" must be an array')
    if not isinstance(include_repo_root, bool):
        raise SystemExit('error: config field "includeRepoRoot" must be a boolean')
    if not isinstance(repo_root_label, str):
        raise SystemExit('error: config field "repoRootLabel" must be a string')
    if accent_color is not None and not isinstance(accent_color, str):
        raise SystemExit('error: config field "accentColor" must be a string')
    if not isinstance(qr_style, dict):
        raise SystemExit('error: config field "qrStyle" must be an object')

    links: list[FileLink] = []
    for entry in external_links:
        if isinstance(entry, str):
            label = entry
            url = entry
        elif isinstance(entry, dict):
            label = entry.get("label")
            url = entry.get("url")
            if not isinstance(label, str) or not isinstance(url, str):
                raise SystemExit('error: externalLinks objects must contain string "label" and "url" fields')
        else:
            raise SystemExit("error: externalLinks entries must be strings or objects")

        if not is_http_url(url):
            raise SystemExit(f"error: external link must start with http:// or https://: {url}")

        links.append(FileLink(label=label, url=url, kind="external"))

    return Config(
        paths=paths,
        external_links=links,
        include_repo_root=include_repo_root,
        repo_root_label=repo_root_label,
        accent_color=accent_color,
        qr_style=normalize_qr_style(qr_style),
    )


def normalize_qr_style(qr_style: dict[str, object]) -> dict[str, str]:
    style = dict(DEFAULT_QR_STYLE)

    for key in style:
        value = qr_style.get(key)
        if value is None:
            continue
        if not isinstance(value, str):
            raise SystemExit(f'error: qrStyle field "{key}" must be a string')
        style[key] = value

    if style["dots"] not in QR_DOT_STYLES:
        raise SystemExit(f'error: qrStyle.dots must be one of: {", ".join(sorted(QR_DOT_STYLES))}')
    if style["cornersSquare"] not in QR_CORNER_SQUARE_STYLES:
        raise SystemExit(
            f'error: qrStyle.cornersSquare must be one of: {", ".join(sorted(QR_CORNER_SQUARE_STYLES))}'
        )
    if style["cornersDot"] not in QR_CORNER_DOT_STYLES:
        raise SystemExit(f'error: qrStyle.cornersDot must be one of: {", ".join(sorted(QR_CORNER_DOT_STYLES))}')

    return style


def normalize_hex_color(value: str) -> str:
    color = value.strip()
    if re.fullmatch(r"#[0-9a-fA-F]{6}", color):
        return color.upper()
    if re.fullmatch(r"[0-9a-fA-F]{6}", color):
        return f"#{color.upper()}"

    raise SystemExit(f"error: accent color must be a 6-digit hex color, got: {value}")


def is_http_url(value: str) -> bool:
    parsed = urlparse(value)
    return parsed.scheme in {"http", "https"} and bool(parsed.netloc)


def discover_files(repo_root: Path, requested_paths: list[str]) -> list[Path]:
    files: list[Path] = []

    for requested in requested_paths:
        path = (repo_root / requested).resolve() if not Path(requested).is_absolute() else Path(requested).resolve()
        try:
            path.relative_to(repo_root)
        except ValueError as exc:
            raise SystemExit(f"error: path is outside the repository: {requested}") from exc

        if path.is_file():
            files.append(path)
        elif path.is_dir():
            files.extend(child for child in path.rglob("*") if child.is_file() and ".git" not in child.parts)
        else:
            raise SystemExit(f"error: path does not exist: {requested}")

    return sorted(set(files), key=lambda item: item.relative_to(repo_root).as_posix())


def quote_path(path: str) -> str:
    return "/".join(quote(part) for part in path.split("/"))


def build_file_links(repo_root: Path, files: list[Path], repo_url: str, branch: str) -> list[FileLink]:
    links: list[FileLink] = []
    for file_path in files:
        relative = file_path.relative_to(repo_root).as_posix()
        url = f"{repo_url}/blob/{quote(branch)}/{quote_path(relative)}"
        links.append(FileLink(label=relative, url=url, kind="github"))

    return links


def render_html(
    links: list[FileLink],
    title: str,
    repo_url: str,
    branch: str,
    qr_size: int,
    display_size: int,
    accent_color: str,
    qr_style: dict[str, str],
) -> str:
    payload = json.dumps([link.__dict__ for link in links], ensure_ascii=False)
    escaped_payload = html.escape(payload, quote=False)
    escaped_title = html.escape(title)
    escaped_repo = html.escape(repo_url)
    escaped_branch = html.escape(branch)
    escaped_qr_size = html.escape(str(qr_size))
    escaped_display_size = html.escape(str(display_size))
    escaped_accent_color = html.escape(accent_color)
    qr_style_payload = json.dumps(qr_style)

    return f"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{escaped_title}</title>
  <style>
    :root {{
      color-scheme: light;
      font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      background: #f6f7f9;
      color: #1d232a;
    }}
    body {{
      margin: 0;
    }}
    main {{
      max-width: 1180px;
      margin: 0 auto;
      padding: 32px 24px 48px;
    }}
    header {{
      margin-bottom: 24px;
    }}
    h1 {{
      margin: 0 0 8px;
      font-size: 28px;
      line-height: 1.2;
    }}
    .meta {{
      margin: 0;
      color: #5a6673;
      font-size: 14px;
    }}
    .grid {{
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 16px;
    }}
    .card {{
      display: grid;
      gap: 12px;
      border: 1px solid #d7dde5;
      border-radius: 8px;
      background: #ffffff;
      padding: 16px;
      break-inside: avoid;
      box-shadow: 0 1px 2px rgba(23, 31, 38, 0.04);
    }}
    .qr {{
      width: {escaped_display_size}px;
      height: {escaped_display_size}px;
      justify-self: center;
    }}
    .qr canvas,
    .qr img {{
      width: {escaped_display_size}px;
      height: {escaped_display_size}px;
      image-rendering: pixelated;
    }}
    .path {{
      overflow-wrap: anywhere;
      font-weight: 650;
      font-size: 14px;
      line-height: 1.35;
    }}
    a {{
      color: {escaped_accent_color};
      text-decoration: none;
    }}
    a:hover {{
      text-decoration: underline;
    }}
    .url {{
      overflow-wrap: anywhere;
      color: #5a6673;
      font-size: 12px;
      line-height: 1.35;
    }}
    .download {{
      justify-self: start;
      border: 1px solid #b8c2cc;
      border-radius: 6px;
      background: #ffffff;
      color: {escaped_accent_color};
      cursor: pointer;
      font: inherit;
      font-size: 13px;
      padding: 8px 10px;
    }}
    .download:hover {{
      background: #eef2f6;
    }}
    @media print {{
      :root {{
        background: #ffffff;
      }}
      main {{
        max-width: none;
        padding: 12mm;
      }}
      .card {{
        box-shadow: none;
      }}
      a {{
        color: #000000;
      }}
      .download {{
        display: none;
      }}
    }}
  </style>
</head>
<body>
  <main>
    <header>
      <h1>{escaped_title}</h1>
      <p class="meta">{len(links)} link(s) from {escaped_repo} on branch {escaped_branch}</p>
    </header>
    <section id="qr-grid" class="grid" aria-label="File QR codes"></section>
  </main>

  <script id="file-links" type="application/json">{escaped_payload}</script>
  <script src="https://cdn.jsdelivr.net/npm/qr-code-styling@1.9.2/lib/qr-code-styling.js"></script>
  <script>
    const links = JSON.parse(document.getElementById("file-links").textContent);
    const grid = document.getElementById("qr-grid");
    const qrSize = {escaped_qr_size};
    const accentColor = "{escaped_accent_color}";
    const qrStyle = {qr_style_payload};

    function fileNameFor(label) {{
      return label
        .toLowerCase()
        .replace(/^https?:\\/\\//, "")
        .replace(/[^a-z0-9]+/g, "-")
        .replace(/^-+|-+$/g, "")
        .slice(0, 96) + ".png";
    }}

    for (const link of links) {{
      const card = document.createElement("article");
      card.className = "card";

      const qr = document.createElement("div");
      qr.className = "qr";

      const path = document.createElement("a");
      path.className = "path";
      path.href = link.url;
      path.textContent = link.label;

      const url = document.createElement("a");
      url.className = "url";
      url.href = link.url;
      url.textContent = link.url;

      const download = document.createElement("button");
      download.className = "download";
      download.type = "button";
      download.textContent = "Download PNG";

      card.append(qr, path, url, download);
      grid.append(card);

      const qrCode = new QRCodeStyling({{
        width: qrSize,
        height: qrSize,
        type: "canvas",
        data: link.url,
        qrOptions: {{
          errorCorrectionLevel: "M"
        }},
        dotsOptions: {{
          color: accentColor,
          type: qrStyle.dots
        }},
        cornersSquareOptions: {{
          color: accentColor,
          type: qrStyle.cornersSquare
        }},
        cornersDotOptions: {{
          color: accentColor,
          type: qrStyle.cornersDot
        }},
        backgroundOptions: {{
          color: "#ffffff"
        }}
      }});
      qrCode.append(qr);

      download.addEventListener("click", () => {{
        qrCode.download({{
          name: fileNameFor(link.label).replace(/\\.png$/, ""),
          extension: "png"
        }});
      }});
    }}
  </script>
</body>
</html>
"""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate an HTML sheet of QR codes for GitHub file links.",
    )
    parser.add_argument(
        "paths",
        nargs="*",
        help="Files or folders to include. Folders are scanned recursively.",
    )
    parser.add_argument(
        "--config",
        type=Path,
        help="Read paths and external links from a JSON config file.",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("qr-codes.html"),
        help="Output HTML file. Defaults to qr-codes.html.",
    )
    parser.add_argument(
        "--repo-url",
        help="GitHub repository URL. Defaults to GITHUB_REPOSITORY or git remote origin.",
    )
    parser.add_argument(
        "--branch",
        default=DEFAULT_BRANCH,
        help=f"GitHub branch to link to. Defaults to {DEFAULT_BRANCH}.",
    )
    parser.add_argument(
        "--title",
        default="presentation-qr-codes",
        help="HTML page title.",
    )
    parser.add_argument(
        "--qr-size",
        type=int,
        default=DEFAULT_QR_SIZE,
        help=f"Rendered QR image size in pixels. Defaults to {DEFAULT_QR_SIZE}.",
    )
    parser.add_argument(
        "--display-size",
        type=int,
        default=DEFAULT_DISPLAY_SIZE,
        help=f"Displayed QR size in CSS pixels. Defaults to {DEFAULT_DISPLAY_SIZE}.",
    )
    parser.add_argument(
        "--accent-color",
        help="6-digit hex color for QR pixels. Overrides config accentColor.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    repo_root = find_repo_root(Path.cwd())

    requested_paths = list(args.paths)
    external_links: list[FileLink] = []
    config_accent_color: str | None = None
    qr_style = dict(DEFAULT_QR_STYLE)
    include_repo_root = False
    repo_root_label = "GitHub repository"
    if args.config:
        config = read_config(args.config)
        requested_paths.extend(config.paths)
        external_links = config.external_links
        config_accent_color = config.accent_color
        qr_style = config.qr_style
        include_repo_root = config.include_repo_root
        repo_root_label = config.repo_root_label

    if not requested_paths and not external_links and not include_repo_root:
        print("error: pass at least one file/folder path or --config", file=sys.stderr)
        return 2

    files = discover_files(repo_root, requested_paths) if requested_paths else []
    if requested_paths and not files:
        print("error: no files found for the requested paths", file=sys.stderr)
        return 2

    repo_url = github_repo_url(args.repo_url, repo_root)
    repo_root_links = [FileLink(label=repo_root_label, url=repo_url, kind="github")] if include_repo_root else []
    links = [*repo_root_links, *build_file_links(repo_root, files, repo_url, args.branch), *external_links]
    if args.qr_size < 128:
        print("error: --qr-size must be at least 128", file=sys.stderr)
        return 2
    if args.display_size < 64:
        print("error: --display-size must be at least 64", file=sys.stderr)
        return 2

    accent_color = normalize_hex_color(args.accent_color or config_accent_color or DEFAULT_ACCENT_COLOR)
    html_text = render_html(
        links,
        args.title,
        repo_url,
        args.branch,
        args.qr_size,
        args.display_size,
        accent_color,
        qr_style,
    )

    output = args.output if args.output.is_absolute() else repo_root / args.output
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(html_text, encoding="utf-8")

    print(f"Generated {output} with {len(links)} QR code(s).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
