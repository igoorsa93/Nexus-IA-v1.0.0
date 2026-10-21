# AGENTS.md — Nexus IA

Este arquivo contém instruções para agentes de IA (Claude Code, Cursor, etc.) que trabalham neste repositório.

## Visão Geral

O Nexus IA é uma suíte de diagnóstico e otimização de PC para jogos, escrita em C# (.NET 8 + WPF). O código-fonte está na pasta `src/` e usa namespace único `NexusIA`.

## Estrutura do Repositório

```
src/            → Código-fonte principal (WPF .NET 8)
build/          → Build publicado (.exe single-file)
assets/         # Recursos visuais (ícones, logos)
docs/           # Documentação (REA, arquitetura)
scripts/        # Scripts utilitários (publicar.bat)
tests/          # Testes (vazio)
```

## Como Compilar

```powershell
# Restaure pacotes
dotnet restore

# Compile
dotnet build -c Release

# Publique como single-file
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish
```

## Convenções de Código

- Namespace único: `NexusIA`
- Linguagem: C# 11 / .NET 8
- Interface: WPF (XAML + code-behind)
- Nomes de classes: PascalCase
- Nomes de métodos: PascalCase
- Nomes de campos privados: camelCase com prefixo `_`
- Comentários em português

## Convenções de Commit

Use Conventional Commits:
- `feat:` nova funcionalidade
- `fix:` correção de bug
- `docs:` documentação
- `style:` formatação, sem mudança de lógica
- `refactor:` refatoração
- `test:` adição de testes
- `chore:` manutenção

Exemplo: `feat: adicionar suporte a OpenRGB para iluminação RGB`

## Arquitetura Principal

| Módulo | Arquivo Principal | Responsabilidade |
|--------|------------------|------------------|
| Sensores | `SensorMonitor.cs` | LibreHardwareMonitor + NVML |
| GPU | `GpuInfoCollector.cs` | WMI, DXGI, NVML |
| Benchmarks | `BenchRenderer.cs`, `DiskBench.cs`, `MemoryBench.cs` | Testes de performance |
| Overlay/FPS | `FrameMonitor.cs` | ETW sem injeção |
| Jogos | `GameLauncher.cs`, `GameLibrary.cs` | Detecção e lançamento |
| Otimização | `Optimizer.cs`, `WindowsTweaks.cs` | Limpeza e tweaks |
| Drivers | `DriverManager.cs`, `MakerDrivers.cs`, `UsbCleaner.cs` | Drivers e USB |
| Iluminação | `LightBackends.cs`, `CorsairSdk.cs` | iCUE, OpenRGB, Razer |

## Como Adicionar uma Nova Funcionalidade

1. Crie a classe em `src/`
2. Adicione a interface na janela principal (`MainWindow.xaml` + partials)
3. Adicione a navegação no menu lateral
4. Teste localmente
5. Faça commit com `feat: descricao`

## Como Corrigir um Bug

1. Identifique o módulo afetado
2. Adicione um teste se aplicável
3. Corrija o código
4. Teste em diferentes versões de Windows se possível
5. Faça commit com `fix: descricao`

## Seções de Interesse para Contribuidores

- **Sensores:** `SensorMonitor.cs` — envolve `LibreHardwareMonitorLib`
- **GPU:** `GpuInfoCollector.cs` — usa WMI, DXGI e NVML
- **Overlay FPS:** `FrameMonitor.cs` — usa TraceEvent (ETW)
- **Iluminação:** `LightBackends.cs` — interfaces para Corsair iCUE, OpenRGB, Razer Chroma

## Recursos Externos

- [LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)
- [Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows) (DirectX bindings)
- [Microsoft.Diagnostics.Tracing.TraceEvent](https://github.com/microsoft/perfview) (ETW)
