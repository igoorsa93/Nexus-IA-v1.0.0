# Nexus IA v1.0.0

Suíte de diagnóstico e otimização de hardware para PC gamers — ferramenta completa de sensores, benchmarks, stress tests, detecção de gargalo, otimização do Windows, gerenciamento de inicialização, drivers seriais e controle de iluminação RGB.

## 🚀 Visão Rápida

| Item | Detalhe |
|------|---------|
| **Nome** | Nexus IA |
| **Versão** | 1.0.0 |
| **Plataforma** | Windows 10/11 64 bits |
| **Tecnologia** | .NET 8, WPF, C# |
| **Licença** | MIT |

## 📋 Funcionalidades

### 🖥️ Diagnóstico de Hardware
- Informações do sistema (SO, placa-mãe, BIOS, CPU, RAM, GPU, discos)
- Sensores em tempo real via LibreHardwareMonitor + NVML (temperatura, carga, volts, ventoinhas, consumo)
- Saúde dos discos S.M.A.R.T.
- Informações de bateria e energia (notebooks)
- Rede (latência, jitter, perda, velocidade, bufferbloat)

### 📊 Benchmarks e Stress Tests
- **CPU Bench:** Path tracer monocromático (modelo Cinebench), single e multi-core
- **GPU Bench:** Cena de jogo Direct3D 11 com terreno fractal e sombras PCF
- **GPU Ray Tracing:** Direct3D 12 DXR 1.1 com DXR 1.1
- **Game Sim Bench:** Simulação de CPU com 30.000 unidades
- **Memory Bench:** Latência e banda de RAM, carregamento de mapa
- **Disk Bench:** Leitura/escrita sequencial e aleatória 4K (CrystalDiskMark-like)
- **Stress Test:** Carga CPU/RAM/GPU com limite de temperatura

### 🎮 Jogos
- Detecção automática de jogos (Steam, Epic Games, GOG, Ubisoft, .exe manual)
- Perfil por jogo (prioridade, plano de energia, GPU dedicada, sem otimizações de tela cheia)
- Argumentos de inicialização (Source, Unity, Unreal)
- Estimativa de FPS via API HowManyFPS ou estimativa local
- Botão JOGAR com aplicação automática de perfil

### 🔧 Otimização do Windows
- Plano de energia Alto desempenho
- Modo de Jogo
- Desativar Game DVR e HAGS
- Limpeza de memória em espera (estilo ISLC)
- Fechamento de programas em segundo plano
- Tweaks reversíveis com backup

### 🚀 Inicialização e Instalação
- Gerenciador de programas de inicialização (ativar/desativar/remover com desfazer)
- Instalador de apps essenciais pós-formatação via winget (Steam, Chrome, Discord, 7-Zip, VLC, etc.)

### 🔌 Drivers Seriais (RESET MAKER)
- Diagnóstico de CH340, CP210x, FTDI, Prolific, Arduino
- Reinstalação e remoção de drivers com backup
- Drivers oficiais embutidos no app
- Limpeza de portas COM "fantasmas"

### 💡 Iluminação RGB (RESET INTEGRAÇÃO)
- Corsair iCUE / iCUE LINK (via SDK oficial)
- OpenRGB (MSI Mystic Light, Gigabyte RGB Fusion, ASUS Aura, etc.)
- Razer Chroma (via API REST do Synapse)
- Efeitos: cor fixa, respiração, arco-íris, temperatura/carga

---

## 📦 Como Baixar e Usar

### Pré-requisitos
- Windows 10 ou 11, 64 bits
- .NET 8 Runtime (incluído no .exe single-file — não precisa instalar)
- **Driver PawnIO** (opcional, para temperatura/consumo da CPU):  
  `winget install namazso.PawnIO`
- Privilégios de administrador (necessário para sensores de baixo nível)

### Executar
```bash
# Baixe o release mais recente das Releases do GitHub
# Extraia o .zip
# Clique com o botão direito em NexusIA.exe → "Executar como administrador"
```

---

## 🛠️ Como Compilar do Código-Fonte

### Pré-requisitos de Desenvolvimento
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (versão 8.0.400 ou superior)
- Windows 10/11 com SDK do Windows

### Comandos
```bash
# Clone o repositório
git clone https://github.com/SEU_USUARIO/NexusIA.git
cd NexusIA

# Restaure pacotes NuGet
dotnet restore

# Compile em modo Release
dotnet build -c Release

# Publique como arquivo único (single-file)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish

# O executável ficará em publish/NexusIA.exe
```

---

## 📁 Estrutura do Projeto

```
NexusIA/
├── src/                    # Código-fonte completo (.NET 8 WPF)
│   ├── App.xaml.cs         # Ponto de entrada, single instance
│   ├── MainWindow.xaml.cs  # Janela principal e orquestração
│   ├── MainWindow.xaml     # Interface WPF principal
│   ├── SensorMonitor.cs    # Sensores de hardware (LibreHardwareMonitor)
│   ├── GpuInfoCollector.cs # Informações da placa de vídeo (WMI/NVML/DXGI)
│   ├── StressTester.cs     # Stress test de CPU/RAM
    ├─ ... (mais módulos)
│
├── build/                  # Build publicado (arquivo único)
│   ├── NexusIA.exe         # Executável principal
│   ├── NexusIA.pdb         # Símbolos de depuração
│   ├── dxcompiler.dll      (dependências do DirectX)
│   ├── dxil.dll
│   ├── MonoPosixHelper.dll
│   └── amd64/
│
├── assets/                 # Recursos visuais
│   ├── app.ico             # Ícone do programa
│   ├── logo.png            # Logo principal
│   ├── logo-badge.png      # Logo para redes sociais
│   ├── logo-dark.png       # Logo versão escura
│   └── NEXUS_IA_LINKEDIN_BANNER.svg  # Banner para redes sociais
│
├── docs/                   # Documentação
│   ├── NEXOS_IA_REA_COMPLETO.md      # Relatório completo de engenharia reversa
│   └── NEXOS_IA_ANALISE_ARQUITETURA.md # Análise detalhada da arquitetura
│
├── scripts/                # Scripts utilitários
│   └── publicar.bat        # Script de publicação
│
├── tests/                  # Testes (vazio — aguardando contribuição)
│
├── .gitignore              # Arquivos ignorados pelo Git
├── .gitattributes          # Configuração de atributos Git
├── LICENSE                 # Licença MIT
└── README.md               # Este arquivo
```

---

## 🧠 Arquitetura Técnica

O projeto é escrito inteiramente em C# com interface WPF (.NET 8). A arquitetura segue namespace único `NexusIA` com separação lógica em módulos:

| Módulo | Responsabilidade |
|--------|------------------|
| `SensorMonitor` | Encapsula LibreHardwareMonitorLib para leitura de sensores |
| `GpuInfoCollector` | Coleta dados da GPU via WMI, DXGI, NVML |
| `FrameMonitor` | Medição de FPS via ETW (sem injeção) |
| `BenchRenderer` | Path tracer de CPU (modelo Cinebench) |
| `StressTester` | Carga sintética de CPU e RAM |
| `Optimizer` | Limpeza de memória e fechamento de processos |
| `WindowsTweaks` | Ajustes reversíveis do Windows |
| `DriverManager` | Inventário e instalação de drivers |
| `UsbCleaner` | Limpeza de dispositivos USB "fantasmas" |
| `MakerDrivers` | Drivers seriais (CH340, CP210x, FTDI, Arduino) |
| `LightBackends` | Pontes para iCUE, OpenRGB, Razer Chroma |
| `ReportBuilder` | Exportação de relatórios HTML/JSON |

---

## 📊 Engenharia Reversa

Este projeto é resultado de uma engenharia reversa detalhada do software RESET EXPLODE v0.16.0. Todo o código foi analisado, documentado e reescrito com arquitetura própria.

- 📄 **Relatório completo:** `docs/NEXOS_IA_REA_COMPLETO.md`
- 📄 **Análise de arquitetura:** `docs/NEXOS_IA_ANALISE_ARQUITETURA.md`

Pontos-chave da REA:
- Não houve ofuscação — todo o código está em CIL legível
- Identidade do assembly: `NexusIA`, versão `1.0.0`, .NET 8
- Arquitetura com namespace único e separação lógica de responsabilidades
- Uso extensivo de P/Invoke para Win32, NVML, DXGI e SetupAPI

---

## ⚠️ Avisos Importantes

1. **Execução como administrador:** O programa requer privilégios elevados para acessar sensores de hardware de baixo nível (temperatura e consumo da CPU via PawnIO).
2. **Driver PawnIO:** É necessário instalar o driver PawnIO para leitura correta de temperatura e consumo da CPU em muitos sistemas.
3. **Driver NVIDIA:** Para leitura avançada de GPU (temperatura, consumo, power limit), é necessário o driver NVIDIA instalado com NVML disponível.
4. **Assinatura digital:** O executável não possui assinatura digital paga. O Windows pode exibir aviso de "O Windows protegeu o computador" na primeira execução — clique em "Mais informações" → "Executar assim mesmo".

---

## 🤝 Como Contribuir

1. Faça um **fork** deste repositório
2. Crie uma branch para sua feature: `git checkout -b feature/minha-feature`
3. Faça suas alterações e teste localmente
4. Envie um **pull request** com uma descrição clara das mudanças

### Diretrizes de Contribuição
- Mantenha o estilo de código existente (C# convencional)
- Teste em diferentes versões de Windows quando possível
- Documente novas funcionalidades no README
- Não inclua binários ou arquivos de build no commit

---

## 📄 Licença

Este projeto está licenciado sob a **Licença MIT**. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

---

## 📞 Contato

- **Autor:** Gustavo Reset / Nexos IA
- **Repositório:** [https://github.com/SEU_USUARIO/NexusIA](https://github.com/SEU_USUARIO/NexusIA)
- **Licença:** MIT

---

*Desenvolvido com 🔧 para entusiastas de hardware e jogadores que querem controlar cada detalhe do seu PC.*
