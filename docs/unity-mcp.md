# Unity MCP Setup

This project uses MCP for Unity through Unity Package Manager:

- Package: `com.coplaydev.unity-mcp`
- Source: `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.0.0`
- Unity version in this project: `2022.3.62f3c1`

Codex is configured to look for the Unity MCP endpoint at:

```toml
[mcp_servers.unityMCP]
url = "http://127.0.0.1:8080/mcp"

[features]
rmcp_client = true
```

After opening the project in Unity, let Package Manager finish resolving packages. Then open the MCP for Unity window from the Unity menu, start/enable the MCP server, and confirm it is listening on `127.0.0.1:8080`.

Quick local check:

```powershell
Get-NetTCPConnection -LocalPort 8080
```
