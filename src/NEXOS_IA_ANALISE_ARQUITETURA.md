# Nexos IA — Relatório de Engenharia Reversa / Análise de Arquitetura

**Base:** repositório `https://github.com/GustavoReset/ResetExplode.git` (clonado localmente).
**Produto original:** RESET EXPLODE v0.16.0 — ferramenta de diagnóstico/otimização de hardware para Windows.
**Novo nome:** Nexos IA.

---

## 1. Resumo executivo

Nexos IA é uma aplicação desktop Windows (WPF) escrita em C# (.NET 8) que agrega diagnóstico de hardware, sensores em tempo real, benchmarks, stress tests, otimização do sistema, gerenciamento de inicialização, detecção de jogos, overlay de FPS e controle de iluminação RGB. Roda como administrador e é distribuída como arquivo único (.exe com .NET embutido) via `publicar.bat` / GitHub Actions.

---

## 2. Ponto de entrada e ciclo de vida

| Elemento | Arquivo | Linha | Detalhe |
|---|---|---|---|
| Classe de aplicação | `App.xaml.cs` | 7 | `public partial class App : Application` |
| Inicialização | `App.OnStartup` | 17 | Mutex de instância única `Local\ResetExplode.SingleInstance`; encontra janela existente via `FindWindow` ("RESET EXPLODE") e traz para a frente, senão cria `new MainWindow().Show()`. |
| Janela principal | `MainWindow.xaml.cs` | 14 | `public partial class MainWindow : Window`; construtor chama `InitializeComponent`, `ThemeManager.Apply`, `ShowPage("PageDash")`, inicia `SensorMonitor`, bench, USB, drives, dashboard. |

**Mutex:** impede múltiplas instâncias. Se outra instância já estiver aberta (mesmo com nível de permissão diferente), busca a janela pelo título "RESET EXPLODE", restaura se minimizada (`ShowWindow SW_RESTORE`) e coloca em primeiro plano (`SetForegroundWindow`), depois encerra a nova instância.

---

## 3. Camadas e módulos (namespace `ResetExplode`)

O código-fonte está todo na raiz do projeto, namespace único `ResetExplode`. A arquitetura se divide em:

### 3.1 UI / Shell (WPF)
- `MainWindow.xaml` + partials `MainWindow.*.cs` (Dash, Disks, Driver, Integ, Launcher, Maker, Net, Oc, Overlay, Rt, Startup) — telas da janela principal.
- `OverlayWindow.xaml` + `OverlayWindow.xaml.cs` — overlay transparente, sempre no topo, ignora cliques; atalho global `Ctrl+Shift+O` (HotkeyId `0x5245` = "RE").
- `ToastWindow.xaml` + `ToastWindow.xaml.cs` — avisos de conexão/remoção de dispositivos USB (`WM_DEVICECHANGE`).
- `Theme.xaml` + `ThemeManager.cs` — paleta escura/clara trocada em tempo de uso via `DynamicResource`; overlay e avisos ficam sempre escuros.
- `App.xaml` — recursos da aplicação.

### 3.2 Sensores e hardware
- `SensorMonitor.cs` — encapsula `LibreHardwareMonitorLib` (`Computer`), expõe `ObservableCollection<SensorRow>`, temperatura/carga/consumo da CPU, diagnóstico de sensores (élevado / PawnIO / falha de leitura).
- `GpuInfoCollector.cs` — monta `GpuInfo` estilo GPU-Z via WMI (`Win32_VideoController`), DXGI, NVML, registro do driver (`HardwareInformation.*`), banco de specs `GpuSpecDb.cs`.
- `GpuSpecDb.cs` — banco estático de specs (NVIDIA Blackwell/Ada/Ampere, AMD RDNA 4/3/2, iGPU) indexado por `VEN:DEV`.
- `SystemInfoCollector.cs` — WMI: SO, placa-mãe/BIOS, CPU, RAM, GPU, discos (`Win32_OperatingSystem`, `Win32_BaseBoard`, `Win32_Processor`, `Win32_PhysicalMemory`, etc.).
- `DriveMap.cs` — unidades montadas (letras) + disco físico por letra, anel de ocupação (`DriveCard.UsedArc`).
- `DiskHealth.cs` — saúde S.M.A.R.T. via WMI Storage (`MSFT_PhysicalDisk`, `MSFT_StorageReliabilityCounter`) + `MSStorageDriver_FailurePredictStatus`.
- `BatteryInfo.cs` — bateria/notebook via WMI (`Win32_Battery`, `BatteryStaticData`, `BatteryFullChargedCapacity`, `BatteryCycleCount`, `BatteryStatus`) + cartão de energia (`Win32_PowerPlan`).
- `Nvml.cs` — P/Invoke mínimo de `nvml.dll` (leitura e escrita: power limit, locked clocks).
- `Dxgi.cs` — enumeração de adaptadores DXGI + feature level Direct3D 12.
- `InfoNode.cs` — nó genérico de seção/valor usado nos relatórios e telas de detalhes.

### 3.3 Benchmarks e stress
- `BenchRenderer.cs` — path tracer monocromático de CPU (modelo Cinebench), 960×540, 1024 spp, buckets do centro para fora, single/multi-core, resultado determinístico.
- `StressTester.cs` — carga CPU (FMA/sqrt/sin + inteiros, todos os threads) e RAM (aloca fração da memória livre, escreve/verifica padrões).
- `GpuBurn.cs` — stress GPU estilo FurMark: torus com pelos via Direct3D 11 (Vortice), HLSL embaralhado, overdraw massivo, janela Win32 própria, sem limite de FPS.
- `GpuGameBench.cs` — cena de jogo Direct3D 11 (terreno fractal, instancing, sombras PCF, HDR).
- `GpuRtBench.cs` / `MainWindow.Rt.cs` — ray tracing Direct3D 12 DXR 1.1, path tracing.
- `GameSimBench.cs` — simulação de CPU de jogo: 30.000 unidades, grade espacial, física/colisão/IA em workers.
- `MemoryBench.cs` — latência de RAM (lista encadeada aleatória, cache misses), banda de leitura/cópia, carregamento de mapa (512 MB + 1000 arquivos 64 KB sem cache do Windows).
- `DiskBench.cs` — CrystalDiskMark-like: seq 1 MB, aleatório 4K QD1/QD32, `FILE_FLAG_NO_BUFFERING`.

### 3.4 Overlay e FPS
- `FrameMonitor.cs` — ETW (TraceEvent) escutando provedores DXGI (`CA11C036...`) e D3D9 (`783ACA0A...`) eventos `Present`; mede FPS de qualquer processo sem injeção (estilo PresentMon).
- `OverlayMetrics.cs` — catálogo de métricas do overlay (FPS, frametime, 1% low, GPU, VRAM, CPU, RAM, jogo, hora).

### 3.5 Detector de gargalo
- `BottleneckAnalyzer.cs` — `LoadSample.Classify()` com regras: VRAM ≥97% → térmico → GPU ≥90% → CPU/núcleo ≥85% → RAM ≥92% → travado. `BottleneckSession` acumula amostras e gera distribuição %.

### 3.6 Jogos (RESET JOGOS)
- `GameCatalog.cs` — catálogo estático de ~35 jogos com FPS de referência (RTX 4070 = 100 GPU, Ryzen 7 7700X = 100 CPU), índices de GPU/CPU, `HowManyFpsClient.cs` para API externa (chave cifrada com DPAPI).
- `GameLibrary.cs`, `GameLauncher.cs` — detecção de jogos instalados (Steam, Epic, GOG, Ubisoft, .exe manual), perfil por jogo (`GameProfile`), prioridade, plano de energia, GPU dedicada, sem otimização tela cheia, argumentos de inicialização (Source/Unity/Unreal).
- `Vdf.cs` — parser/serializador de VDF/ACF da Steam (`localconfig.vdf`).
- `MainWindow.Launcher.cs` — botão JOGAR aplica perfil, inicia e desfaz temporários ao fechar.

### 3.7 Otimização e tweaks (RESET OTIMIZA)
- `Optimizer.cs` — lista processos em segundo plano, fecha (CloseMainWindow / Kill), limpa lista de espera da RAM (`NtSetSystemInformation SystemMemoryListInformation = 4`), `EmptyWorkingSet`, ajuste de privilégio `SeProfileSingleProcessPrivilege`.
- `WindowsTweaks.cs` — ajustes reversíveis do Windows: plano de energia Alto desempenho, Modo de Jogo, Game DVR, HAGS, etc., com backup em `AppSettings.TweakBackup` e "Restaurar originais".

### 3.8 Inicialização e instalação
- `StartupManager.cs` — chaves `Run` (HKCU/HKLM, 32/64 bits) + pastas Inicializar; `StartupApproved` para ativar/desativar; remoção com backup (`%AppData%\ResetExplode\Startup`).
- `AppInstaller.cs` — catálogo pós-formatação (Steam, Chrome, Discord, Lightshot, 7-Zip, WinRAR, VLC) via `winget --silent`; detecção de instalados pelo registro `Uninstall`.

### 3.9 Drivers e maker (RESET MAKER / RESET DRIVER)
- `DriverManager.cs` — inventário de drivers (`Win32_PnPSignedDriver`, WMI), status de driver de vídeo NVIDIA/AMD/Intel, download via HttpClient, páginas oficiais.
- `MakerDrivers.cs` — diagnóstico de chips seriais (CH340, CP210x, FTDI, Prolific, Arduino) por VID/PID, problema do Gerenciador de Dispositivos, reinstalação/remoção com backup, drivers oficiais embutidos em `Drivers\<chip>`.
- `UsbCleaner.cs` — `SetupDiGetClassDevs`/`SetupDiEnumDeviceInfo` (inclui dispositivos ausentes/"fantasmas"), limpeza de reservas COM, `CM_Get_DevNode_Status`.

### 3.10 Integração de iluminação (RESET INTEGRAÇÃO)
- `LightBackends.cs` — interface `ILightBackend` + `LightDevice`; backends: `CorsairBackend` (iCUE SDK), `OpenRgbBackend` (TCP localhost:6742, protocolo v3), Razer Chroma (API REST).
- `CorsairSdk.cs` — P/Invoke de `iCUESDK.x64_2019.dll` (conectar, dispositivos, posições de LED, set colors, request/release control).

### 3.11 Relatórios e configuração
- `ReportBuilder.cs` — exportação HTML/JSON de sistema, GPUs, discos, energia, rede, sensores, benchmarks; remove números de série por privacidade.
- `AppSettings.cs` — `%AppData%\ResetExplode\settings.json` (JSON); chave da API HowManyFPS cifrada com `ProtectedData` (DPAPI, `CurrentUser`); backup de tweaks, COMDB, startups removidos, perfis de jogos, bench runs/records.

---

## 4. Dependências externas (NuGet / pacotes)

| Pacote | Versão | Uso |
|---|---|---|
| `LibreHardwareMonitorLib` | 0.9.6 | Sensores de hardware (temperatura, carga, volts, ventoinhas) |
| `Microsoft.Diagnostics.Tracing.TraceEvent` | 3.2.8 | ETW para medição de FPS (`FrameMonitor`) |
| `System.Management` | 10.0.2 | WMI (SO, placa-mãe, CPU, RAM, GPU, discos, bateria, energia) |
| `System.Security.Cryptography.ProtectedData` | 10.0.2 | Cifragem da chave da API (DPAPI) |
| `Vortice.D3DCompiler` | 3.8.3 | Compilação de shaders HLSL |
| `Vortice.Direct3D11` | 3.8.3 | Stress GPU (`GpuBurn`) |
| `Vortice.Direct3D12` | 3.8.3 | Ray tracing (`GpuRtBench`) |
| `Vortice.Dxc` | 3.8.3 | Compilador de shaders DXC |

**Driver opcional:** PawnIO (`winget install namazso.PawnIO`) — leitura de temperatura/consumo da CPU via MSR/SMN (substitui WinRing0 bloqueado pelo Defender).

**DLL nativa opcional:** `Native\iCUESDK.x64_2019.dll` — iCUE SDK v4 da Corsair (não versionada no git; baixada em runtime se presente).

**Drivers seriais embutidos:** `Drivers\CH340`, `Drivers\CP210x`, `Drivers\FTDI`, `Drivers\Arduino` (`.inf/.cat/.sys`) — extraídos como `EmbeddedResource`.

---

## 5. Interop Win32 / P/Invoke notável

- `user32.dll`: `FindWindow`, `ShowWindow`, `IsIconic`, `SetForegroundWindow`, `RegisterClassExW`, `CreateWindowExW`, `DefWindowProcW`, `PeekMessageW`, `TranslateMessage`, `DispatchMessageW`, `DestroyWindow`, `GetClientRect`, `AdjustWindowRect`, `SetWindowTextW`, `GetSystemMetrics`, `LoadCursorW`, `GetForegroundWindow`, `GetWindowThreadProcessId`.
- `kernel32.dll`: `GetModuleHandleW`.
- `psapi.dll`: `EmptyWorkingSet`.
- `ntdll.dll`: `NtSetSystemInformation` (purge standby list).
- `advapi32.dll`: `OpenProcessToken`, `LookupPrivilegeValue`, `AdjustTokenPrivileges`, `CloseHandle`.
- `dxgi.dll` / `d3d12.dll`: COM interfaces `IDXGIFactory1`, `IDXGIAdapter1`, `D3D12CreateDevice`.
- `nvml.dll`: leitura/escrita de parâmetros de GPU NVIDIA.
- `iCUESDK.x64_2019.dll`: controle de iluminação Corsair.

---

## 6. Fluxo de inicialização (resumo)

1. `App.OnStartup` → verifica mutex single-instance → se for a primeira, cria `MainWindow`.
2. `MainWindow` construtor → aplica tema, mostra página `PageDash`, inicia `SensorMonitor.OpenAsync()` (detecção de hardware em background), `InitBench()`, `SnapshotUsb()`, `RefreshDrives()`, `UpdateDashboard()`.
3. Sensores preenchem `SensorMonitor.Rows` agrupados por `Hardware`; dashboard exibe CPU×GPU, anéis RAM/VRAM/disco, eventos recentes, saúde do PC.

---

## 7. Persistência e locais de dados

| Caminho | Conteúdo |
|---|---|
| `%AppData%\ResetExplode\settings.json` | Configurações (tema, overlay, perfis de jogos, backup de tweaks, chave API cifrada) |
| `%AppData%\ResetExplode\DriversSeriais\` | Backup de drivers seriais removidos |
| `%AppData%\ResetExplode\Startup\` | Atalhos de inicialização removidos (para desfazer) |
| `Drivers\<chip>\*.inf/.cat/.sys` | Drivers seriais oficiais embutidos no .exe (recurso) |

---

## 8. Pontos de atenção para rebranding "Nexos IA"

1. **Título da janela / mutex:** `App.xaml.cs` usa `"Local\ResetExplode.SingleInstance"` e `FindWindow(null, "RESET EXPLODE")`; `MainWindow` define `Title`. Alterar para "Nexos IA" em todos os locais.
2. **Nome do assembly:** `ResetExplode.csproj` `<AssemblyName>ResetExplode</AssemblyName>`, `<Product>RESET EXPLODE</Product>`, versão `0.16.0`.
3. **Pasta de dados:** `AppSettings.Folder` = `%AppData%\ResetExplode`; renomear para `%AppData%\Nexos IA`.
4. **Ícone e logo:** `Assets\app.ico`, `Assets\logo.png`, `logo-badge.png`, `logo-dark.png` (recursos do WPF).
5. **Assinatura:** o .exe original não tem assinatura digital paga (Windows mostra "O Windows protegeu o computador"). Para Nexos IA, considerar assinatura.
6. **Driver PawnIO:** dependência externa para sensores de CPU; manter documentação.

---

## 9. Licença

MIT (arquivo `LICENSE`). Contribuições via fork; não aceita pull requests no repositório original. Manter aviso de copyright ao redistribuir, especialmente `LibreHardwareMonitorLib` (MPL 2.0).

---

## 10. Conclusão

Nexos IA (base ResetExplode v0.16.0) é uma suíte completa de diagnóstico e otimização de PC para jogos, com forte ênfase em leitura de baixo nível de sensores (WMI + LibreHardwareMonitor + NVML + PawnIO), medição de FPS não-injetiva via ETW, benchmarks determinísticos de CPU/GPU/disco/RAM, e integração de iluminação RGB. O código-fonte está disponível, não há binário no repositório (compilação via `dotnet build` / `publicar.bat` com .NET 8 SDK).
