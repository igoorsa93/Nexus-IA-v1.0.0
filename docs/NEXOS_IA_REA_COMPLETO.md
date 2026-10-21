# Nexos IA — Relatório de Engenharia Reversa (REA) Completo

**Produto original:** RESET EXPLODE v0.16.0 (repositório `https://github.com/GustavoReset/ResetExplode.git`)
**Novo nome:** Nexos IA
**Plataforma:** Windows 10/11 64 bits, .NET 8, WPF
**Arquivo analisado:** `ResetExplode.dll` (build Release, single-file host `ResetExplode.exe`)
**SHA-256 DLL:** `1ca2f225a92c5482949bcbf7daa99490e3787511e7ce820cd7df57c5c49a8398`
**MVID:** `de5b1cbc-0dff-4dd8-aef3-8a566c306c2e`

---

## 1. Resumo executivo

Nexos IA (base ResetExplode) é uma suíte de diagnóstico e otimização de PC para jogos, escrita inteiramente em C# (.NET 8) com interface WPF. Reúne: leitura de sensores de hardware (temperatura, carga, volts, ventoinhas), benchmarks determinísticos (CPU/GPU/disco/RAM), stress tests, medição de FPS não-injetiva via ETW, detector de gargalo, gerenciador de inicialização do Windows, instalador de apps pós-formatação via winget, diagnóstico de drivers e portas seriais (RESET MAKER), controle de iluminação RGB (iCUE/OpenRGB/Razer), e relatórios exportáveis HTML/JSON.

Rodará como administrador. É distribuído como .exe single-file com .NET embutido via `dotnet publish -p:PublishSingleFile=true`.

---

## 2. Ponto de entrada e ciclo de vida

| Elemento | Local | Detalhe |
|---|---|---|
| Assembly gerenciado | `ResetExplode.dll` | PE32+ x86_64, CIL/IL-only, runtime v4.0.30319, TargetFramework `.NETCoreApp,Version=v8.0`, Assembly versão `0.16.0.0`, sem strong name |
| Host nativo | `ResetExplode.exe` | PE single-file (sem CLI directory — é o bootstrap .NET que carrega o DLL) |
| Classe App | `ResetExplode.App` | Mutex single-instance `Local\ResetExplode.SingleInstance`; `FindWindow(null, "RESET EXPLODE")` traz janela existente para a frente; senão cria `new MainWindow().Show()` |
| Janela principal | `ResetExplode.MainWindow` | Estende `System.Windows.Window`; 486 campos, 266 métodos |
| Página inicial | `ShowPage("PageDash")` | Dashboard com gráfico CPU×GPU, anéis RAM/VRAM/disco, eventos recentes |

**Fluxo de inicialização:**
1. `App.OnStartup` → verifica mutex `Local\ResetExplode.SingleInstance`
2. Se já existe instância: `FindWindow("RESET EXPLODE")` → `ShowWindow(SW_RESTORE)` + `SetForegroundWindow` → encerra nova instância
3. Se primeira instância: `new MainWindow().Show()`
4. `MainWindow` construtor → `ThemeManager.Apply`, `ShowPage("PageDash")`, inicia `SensorMonitor.OpenAsync()` (detecção hardware em background), `InitBench()`, `SnapshotUsb()`, `RefreshDrives()`, `UpdateDashboard()`

---

## 3. Identidade do assembly (metadados CLI)

```
PE32+ x86_64, 2 seções, CLI header @ offset 512 (tamanho 72 bytes)
Flags: IL-only
Entry point: metadata token 0x0600003f (App.Main)
Runtime version: v4.0.30319
Metadata streams: #Blob, #GUID, #Strings, #US, #~
TargetFramework: .NETCoreApp,Version=v8.0
Assembly: ResetExplode, Version=0.16.0.0, Culture=neutral, PublicKeyToken=null
MVID: de5b1cbc-0dff-4dd8-aef3-8a566c306c2e
```

**Contagens de tabelas de metadados:**
| Tabela | Linhas |
|---|---|
| TypeDef | 452 |
| MethodDef | 3.320 |
| Field | 2.684 |
| TypeRef | 641 |
| MemberRef | 2.469 |
| CustomAttribute | 4.547 |
| Property | 646 |
| Event | 7 |
| AssemblyRef | 52 |
| ManifestResource | 42 |
| Call edges (estáticas) | 20.612 |
| Field accesses | 11.015 |
| User strings (#US) | 2.912 |

---

## 4. Arquitetura de módulos (namespace `ResetExplode`)

Os 452 TypeDef incluem tipos do próprio projeto + compilados (anonymous types, display classes, state machines de async). Abaixo, os tipos **próprios do projeto** mapeados por responsabilidade:

### 4.1 UI / Shell (WPF)
| Tipo | Tipo | Campos | Métodos | Responsabilidade |
|---|---|---|---|---|
| `ResetExplode.App` | class | 2 | 8 | Single-instance mutex, ponto de entrada |
| `ResetExplode.MainWindow` | class | 486 | 266 | Janela principal; shell com todas as páginas (partials no XAML) |
| `ResetExplode.OverlayWindow` | class (Window) | 29 | 30 | Overlay transparente sempre-no-topo, ignora cliques; FPS em jogos |
| `ResetExplode.ToastWindow` | class (Window) | 5 | 12 | Avisos de conexão/remoção USB (`WM_DEVICECHANGE`) |
| `ResetExplode.ThemeManager` | static class | 2 | 4 | Troca de tema escuro/claro em tempo de uso (`DynamicResource`) |

### 4.2 Sensores e hardware
| Tipo | Responsabilidade |
|---|---|
| `ResetExplode.SensorMonitor` | Encapsula `LibreHardwareMonitorLib`; `ObservableCollection<SensorRow>`; `CpuTemperature`, `CpuLoad`, `CpuPower`; diagnóstico de sensores (elevado / PawnIO ausente / falha de leitura); chave de registro `PawnIO` |
| `ResetExplode.SensorRow` | Linha de sensor (Id, Hardware, Category, Name, Type, HardwareType, valor/mín/máx) |
| `ResetExplode.GpuInfoCollector` | Coleta `GpuInfo` estilo GPU-Z via WMI `Win32_VideoController`, DXGI, NVML, registro do driver (`HardwareInformation.*`) |
| `ResetExplode.GpuInfo` | Dados de uma GPU: chip, arquitetura, VRAM, clocks, barramento PCIe, energia, driver, VBIOS, CUDA, tecnologias |
| `ResetExplode.GpuSpecDb` | Banco estático de specs (NVIDIA Blackwell/Ada/Ampere, AMD RDNA 4/3/2, iGPU) indexado por `VEN:DEV` |
| `ResetExplode.GpuSpec` | Registro de especificação de GPU (chip, die, transistores, shaders, ROPs/TMUs, memória, clocks, TDP) |
| `ResetExplode.SystemInfoCollector` | WMI: SO, placa-mãe/BIOS (`Win32_BaseBoard`, `Win32_BIOS`), CPU (`Win32_Processor`), RAM, GPU, discos |
| `ResetExplode.DriveMap` | Unidades montadas (letras) + disco físico por letra; anel de ocupação (`UsedArc`) |
| `ResetExplode.DiskHealth` | Saúde S.M.A.R.T. via WMI Storage (`MSFT_PhysicalDisk`, `MSFT_StorageReliabilityCounter`, `MSStorageDriver_FailurePredictStatus`) |
| `ResetExplode.BatteryInfo` | Bateria/notebook via WMI (`Win32_Battery`, `BatteryStaticData`, `BatteryFullChargedCapacity`, `BatteryCycleCount`, `BatteryStatus`) + cartão de energia (`Win32_PowerPlan`) |
| `ResetExplode.Nvml` | P/Invoke mínimo de `nvml.dll` (leitura e **escrita**: power limit, locked clocks — RESET OC) |
| `ResetExplode.Dxgi` | Enumeração de adaptadores DXGI + feature level Direct3D 12 (`D3D12CreateDevice`) |

### 4.3 Benchmarks e stress
| Tipo | Responsabilidade |
|---|---|
| `ResetExplode.BenchRenderer` | Path tracer monocromático de CPU (modelo Cinebench), 960×540, 1024 spp, buckets centro-para-fora, single/multi-core, resultado determinístico |
| `ResetExplode.BenchResult` | Resultado do bench: pontos = amostras/s ÷ 10.000 |
| `ResetExplode.StressTester` | Carga CPU (FMA/sqrt/sin + inteiros, todos os threads lógicos) e RAM (aloca fração da memória livre, escreve/verifica padrões) |
| `ResetExplode.GpuBurn` | Stress GPU estilo FurMark: torus com pelos via Direct3D 11 (Vortice), HLSL embaralhado, overdraw massivo, janela Win32 própria, sem limite de FPS |
| `ResetExplode.GpuBurnSettings` | Configurações do stress GPU (resolução, MSAA, shells, iterações, adapter) |
| `ResetExplode.GpuGameBench` | Cena de jogo Direct3D 11 (terreno fractal, instancing, sombras PCF, água, HDR) |
| `ResetExplode.GpuRtBench` | Ray tracing Direct3D 12 DXR 1.1, path tracing, TLAS refeita por frame |
| `ResetExplode.GameSimBench` | Simulação de CPU de jogo: 30.000 unidades, grade espacial, física/colisão/IA em workers |
| `ResetExplode.MemoryBench` | Latência de RAM (lista encadeada aleatória, cache misses), banda leitura/cópia, carregamento de mapa (512 MB + 1000 arquivos 64 KB sem cache do Windows) |
| `ResetExplode.DiskBench` | CrystalDiskMark-like: seq 1 MB, aleatório 4K QD1/QD8, `FILE_FLAG_NO_BUFFERING` + `WRITE_THROUGH` |

### 4.4 Overlay e FPS (ETW, não-injetivo)
| Tipo | Responsabilidade |
|---|---|
| `ResetExplode.FrameMonitor` | ETW (TraceEvent) escuta provedores DXGI (`CA11C036...`) e D3D9 (`783ACA0A...`) eventos `Present`; mede FPS/1% low de qualquer processo sem injeção (estilo PresentMon) |
| `ResetExplode.FrameStats` | Estatísticas de frame: FPS, frametime, 1% low, frame times recentes |
| `ResetExplode.OverlayMetrics` | Catálogo de métricas do overlay (FPS, frametime, 1% low, gráfico, GPU, VRAM, CPU, RAM, jogo, hora) |
| `ResetExplode.OverlayMetric` / `OverlayLine` | Definição e linha pronta de exibição de métrica |

### 4.5 Detector de gargalo
| Tipo | Responsabilidade |
|---|---|
| `ResetExplode.BottleneckAnalyzer` | `LoadSample.Classify()` com regras: VRAM ≥97% → térmico → GPU ≥90% → CPU/núcleo ≥85% → RAM ≥92% → travado. `Balance()` equilíbrio teórico GPU×CPU |
| `ResetExplode.LoadSample` | Leitura 1/s: carga GPU, CPU total/núcleo mais ocupado, VRAM%, RAM%, temperaturas, thermal cap, FPS |
| `ResetExplode.BottleneckSession` | Acumula amostras durante sessão de jogo; distribuição % por limitador; FPS médio/máximo |

### 4.6 Jogos (RESET JOGOS)
| Tipo | Responsabilidade |
|---|---|
| `ResetExplode.GameCatalog` | Catálogo estático de ~35 jogos com FPS de referência (RTX 4070 = 100 GPU, Ryzen 7 7700X = 100 CPU), índices de GPU/CPU |
| `ResetExplode.GameInfo` / `FpsEstimate` / `Bottleneck` | Modelo de dados de estimativa de FPS |
| `ResetExplode.HowManyFpsClient` | Cliente da API HowManyFPS (chave cifrada com DPAPI em `%AppData%\ResetExplode`) |
| `ResetExplode.GameLibrary` / `ResetExplode.GameLauncher` | Detecção de jogos instalados (Steam, Epic, GOG, Ubisoft, .exe manual); perfil por jogo; botão JOGAR |
| `ResetExplode.GameProfile` | Perfil por jogo: prioridade, plano de energia, GPU dedicada, sem otimização tela cheia, modo de janela, resolução, Hz, estilo (source/unity/unreal), argumentos extras, programas auxiliares |
| `ResetExplode.CustomGame` | Jogo adicionado à mão (.exe qualquer) |
| `ResetExplode.HelperProgram` | Programa auxiliar que abre antes e fecha junto com o jogo |
| `ResetExplode.SteamInfo` / `ResetExplode.LibGame` | Detecção de bibliotecas Steam/Epic/GOG/Ubisoft |
| `ResetExplode.Vdf` / `ResetExplode.VdfNode` | Parser/serializador de VDF/ACF da Steam (`localconfig.vdf` com backup) |

### 4.7 Otimização e tweaks (RESET OTIMIZA)
| Tipo | Responsabilidade |
|---|---|
| `ResetExplode.Optimizer` | Lista processos em segundo plano, fecha (CloseMainWindow / Kill), limpa lista de espera RAM (`NtSetSystemInformation SystemMemoryListInformation=4`), `EmptyWorkingSet`, privilégio `SeProfileSingleProcessPrivilege` |
| `ResetExplode.WindowsTweaks` | Ajustes reversíveis do Windows: plano Alto desempenho, Modo de Jogo, Game DVR, HAGS, etc.; backup em `AppSettings.TweakBackup`; "Restaurar originais" |
| `ResetExplode.Tweak` | Definição de um ajuste (leitura/escrita/restauração via registro ou powercfg) |

### 4.8 Inicialização e instalação de apps
| Tipo | Responsabilidade |
|---|---|
| `ResetExplode.StartupManager` | Chaves `Run` (HKCU/HKLM 32/64 bits) + pastas Inicializar; `StartupApproved` para ativar/desativar; remoção com backup (`%AppData%\ResetExplode\Startup`) |
| `ResetExplode.StartupEntry` / `StartupBackup` | Entrada de inicialização e backup para desfazer |
| `ResetExplode.AppInstaller` | Catálogo pós-formatação (Steam, Chrome, Discord, Lightshot, 7-Zip, WinRAR, VLC) via `winget --silent`; detecção de instalados pelo registro `Uninstall` |
| `ResetExplode.InstallableApp` | Item do catálogo (nome, descrição, winget ID, prefixo de exibição, estado instalado) |

### 4.9 Drivers e portas seriais (RESET MAKER / RESET DRIVER)
| Tipo | Responsabilidade |
|---|---|
| `ResetExplode.DriverManager` | Inventário de drivers (`Win32_PnPSignedDriver`, WMI), status de driver de vídeo NVIDIA/AMD/Intel, download via HttpClient, páginas oficiais |
| `ResetExplode.DriverRow` / `GpuDriverStatus` / `BoardInfo` / `WuDriver` | Linhas de inventário e status |
| `ResetExplode.MakerDrivers` | Diagnóstico de chips seriais (CH340, CP210x, FTDI, Prolific, Arduino) por VID/PID, problema do Gerenciador de Dispositivos, reinstalação/remoção com backup, drivers oficiais embutidos em `Drivers\<chip>` |
| `ResetExplode.SerialChip` / `SerialDeviceRow` / `BundledDriver` / `SerialDriverPackage` | Modelo de dados de chips e drivers seriais |
| `ResetExplode.UsbCleaner` | `SetupDiGetClassDevs`/`SetupDiEnumDeviceInfo` (inclui dispositivos ausentes/"fantasmas"); limpeza de reservas COM; `CM_Get_DevNode_Status` |
| `ResetExplode.UsbDevice` | Dispositivo USB/serial registrado (conectado ou fantasma) |

### 4.10 Integração de iluminação (RESET INTEGRAÇÃO)
| Tipo | Responsabilidade |
|---|---|
| `ResetExplode.ILightBackend` | Interface de ponte de iluminação (Acquire/Send/Release/EndSession) |
| `ResetExplode.LightDevice` | Dispositivo de iluminação traduzido (backend, ID, nome, posições LED) |
| `ResetExplode.CorsairBackend` / `CorsairSdk` | iCUE SDK v4 (P/Invoke de `iCUESDK.x64_2019.dll`); conectar, dispositivos, posições de LED, set colors, request/release control |
| `ResetExplode.CorsairDevice` | Dispositivo Corsair (tipo, LEDs, posições) |
| `ResetExplode.OpenRgbBackend` | OpenRGB via TCP localhost:6742, protocolo v3 (cobre MSI/Gigabyte/ASUS/Razer/Logitech) |
| `ResetExplode.RazerChromaBackend` | Razer Chroma via API REST do Synapse |
| `ResetExplode.LightDeviceVm` | ViewModel de dispositivo de iluminação |

### 4.11 Relatórios e configuração
| Tipo | Responsabilidade |
|---|---|
| `ResetExplode.ReportBuilder` | Exportação HTML/JSON de sistema, GPUs, discos, energia, rede, sensores, benchmarks; remove números de série por privacidade |
| `ResetExplode.ReportData` / `ReportNode` / `ReportGpu` / `ReportDisk` / `ReportSensor` / `ReportBench` | Modelo de dados do relatório |
| `ResetExplode.AppSettings` | `%AppData%\ResetExplode\settings.json` (JSON); chave HowManyFPS cifrada com `ProtectedData` (DPAPI, CurrentUser); backup de tweaks, COMDB, startups removidos, perfis de jogos, bench runs/records |
| `ResetExplode.BenchRun` / `BenchRecord` | Histórico de benchmarks |

### 4.12 Interop Win32 / P/Invoke (tipos de suporte)
| Tipo | Responsabilidade |
|---|---|
| `ResetExplode.Nvml` | P/Invoke de `nvml.dll` |
| `ResetExplode.Dxgi` / `Adapter` / `AdapterDesc1` / `IDXGIFactory1` / `IDXGIAdapter1` | COM DXGI |
| `ResetExplode.WNDCLASSEX` / `WndProc` / `MSG` / `RECT` / `Mat` / `Sphere` / `Camera` / `Vertex` / `Constants` | Estruturas Win32 e shaders (GpuBurn) |
| `ResetExplode.WINTRUST_FILE_INFO` / `WINTRUST_DATA` | Verificação de assinatura (driver iCUE) |
| `ResetExplode.ConnectFn` / `NoArgFn` / `GetDevicesFn` / ... / `StateCb` | Delegates de função native (iCUE SDK) |

---

## 5. Strings de usuário (#US) relevantes encontradas

| String | Contexto |
|---|---|
| `Local\ResetExplode.SingleInstance` | Mutex de instância única |
| `RESET EXPLODE` | Título da janela (busca via `FindWindow`) |
| `/ResetExplode;component/app.xaml` | URI do recurso XAML da aplicação |
| `Steam`, `Valve.Steam` | Detecção de jogos Steam |
| `Microsoft\WindowsApps\winget.exe` | Caminho do winget |
| `Software\Microsoft\Windows\CurrentVersion\Uninstall` | Registro para detecção de apps instalados |
| `settings.json` | Arquivo de configurações |
| `ResetExplode` | Nome da pasta `%AppData%` |

> **Nota:** Para rebranding para "Nexos IA", estas strings precisam ser alteradas no código-fonte e recompiladas (não foram encontradas strings "Nexos" no binário atual).

---

## 6. Call edges destacadas (estáticas)

A página 1 de call edges (64 de 20.612 totais) mostra principalmente calls de tipos anônimos compilados (`<>f__AnonymousType*`) para `System.Object`, `System.Collections.Generic.EqualityComparer`1`, `System.String.Format`. Exemplos de chamadas reais do projeto (do método `App.OnStartup`):

- `newobj` → `System.Threading.Mutex..ctor`
- `call` → `App.FindWindow` (P/Invoke `user32.FindWindow`)
- `call` → `App.IsIconic` (P/Invoke `user32.IsIconic`)
- `call` → `App.ShowWindow` (P/Invoke `user32.ShowWindow`)
- `call` → `App.SetForegroundWindow` (P/Invoke `user32.SetForegroundWindow`)
- `newobj` → `ResetExplode.MainWindow..ctor`
- `call` → `MainWindow.Show`

Para mapear o grafo completo de chamadas entre as 452 types e 20.612 call edges, são necessárias páginas adicionais (offset 64, 128, ... até 20.612).

---

## 7. Pontos de atenção para rebranding "Nexos IA"

| Item | Local | Ação |
|---|---|---|
| Título da janela / mutex | `App.xaml.cs`: `Local\ResetExplode.SingleInstance`, `FindWindow(null, "RESET EXPLODE")` | Alterar para `Local\NexosIA.SingleInstance`, `"NEXOS IA"` |
| Nome do assembly | `ResetExplode.csproj`: `<AssemblyName>ResetExplode</AssemblyName>`, `<Product>RESET EXPLODE</Product>`, versão `0.16.0` | Alterar para `NexosIA`, `NEXOS IA`, versão desejada |
| Pasta de dados | `AppSettings.Folder`: `%AppData%\ResetExplode` | Alterar para `%AppData%\Nexos IA` |
| Arquivo de configurações | `AppSettings.FilePath`: `settings.json` | Manter ou renomear |
| Ícone e logo | `Assets\app.ico`, `Assets\logo.png`, `logo-badge.png`, `logo-dark.png` | Substituir por assets da Nexos IA |
| URI do XAML | String `/ResetExplode;component/app.xaml` | Alterar para `/NexosIA;component/app.xaml` |
| Assinatura digital | O .exe original não tem assinatura paga (Windows exibe aviso) | Considerar assinar o .exe da Nexos IA |
| Driver PawnIO | Dependência externa para sensores de CPU | Manter documentação |
| DLL iCUE | `Native\iCUESDK.x64_2019.dll` opcional | Manter ou remover se não usar iluminação Corsair |

---

## 8. Licença e distribuição

- Licença original: **MIT** (arquivo `LICENSE`). Manter aviso de copyright ao redistribuir.
- `LibreHardwareMonitorLib`: **MPL 2.0** — manter aviso de licença.
- `Vortice.Windows`: **MIT**.
- `Microsoft.Diagnostics.Tracing.TraceEvent`: **MIT**.
- Driver **PawnIO**: gratuito, assinado (opcional, para sensores de CPU).
- O repositório original **não aceita pull requests**; contribua via fork.

---

## 9. Conclusão

A engenharia reversa do ResetExplode (rebatizado **Nexos IA**) foi concluída com sucesso:

1. ✅ Clonado o repositório GitHub
2. ✅ Instalado .NET 8 SDK (8.0.426)
3. ✅ Compilado o projeto (`dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true`)
4. ✅ Gerado `ResetExplode.exe` (16 MB, host nativo single-file) e `ResetExplode.dll` (5,1 MB, assembly gerenciado)
5. ✅ REA estática do assembly gerenciado: identidade PE/CLI, 452 tipos, 3.320 métodos, 2.684 campos, 20.612 call edges, 11.015 field accesses, 2.912 user strings
6. ✅ Páginas detalhadas extraídas: types (offsets 0, 64, 128), call_edges (offset 0), fields (offset 0), methods e user_strings (processados via agente)

O código-fonte está disponível e documentado no README original. A análise estática confirmou que não há ofuscação, empacotamento ou código nativo oculto além do host .NET single-file — toda a lógica está em CIL legível no `ResetExplode.dll`.

**Arquivos gerados nesta sessão:**
- `C:\Users\igoor\AppData\Local\Temp\opencode\reset-explode-dist\ResetExplode.exe` (host)
- `C:\Users\igoor\AppData\Local\Temp\opencode\reset-explode\bin\Release\net8.0-windows\win-x64\ResetExplode.dll` (assembly gerenciado)
- `C:\Users\igoor\AppData\Local\Temp\opencode\reset-explode\NEXOS_IA_ANALISE_ARQUITETURA.md` (relatório de arquitetura do código-fonte)
