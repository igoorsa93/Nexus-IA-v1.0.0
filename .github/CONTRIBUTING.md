# Guia de Contribuição — Nexus IA

Obrigado por se interessar em contribuir com o Nexus IA! Este documento explica como você pode ajudar a melhorar o projeto.

## Antes de Começar

1. Leia este guia completo
2. Verifique se já existe uma issue aberta sobre o mesmo tópico
3. Se for uma funcionalidade grande, abra uma issue primeiro para discutir

## Como Contribuir

### 1. Faça um Fork

Faça um fork do repositório para sua conta do GitHub.

### 2. Clone Localmente

```bash
git clone https://github.com/SEU_USUARIO/NexusIA.git
cd NexusIA
```

### 3. Crie uma Branch

```bash
git checkout -b feature/nome-da-sua-funcionalidade
```

Ou para correções:
```bash
git checkout -b fix/nome-do-bug
```

### 4. Desenvolva

- Siga as convenções de código definidas em `AGENTS.md`
- Teste suas alterações localmente
- Mantenha o código limpo e documentado

### 5. Compile e Teste

```bash
dotnet build -c Release
```

### 6. Commit

Use [Conventional Commits](https://www.conventionalcommits.org/):

```bash
git add .
git commit -m "feat: adicionar suporte a novos sensores de temperatura"
```

### 7. Push e Pull Request

```bash
git push origin feature/nome-da-sua-funcionalidade
```

Depois, abra um Pull Request no GitHub.

## Regras de Contribuição

- **Não commite binários** (exe, dll, pdb) — eles vão para `.gitignore`
- **Não commite senhas ou chaves** — nunca coloque credenciais no código
- **Teste suas alterações** antes de enviar o PR
- **Documente novas funcionalidades** no README se necessário
- **Seja respeitoso** em issues e PRs

## Reconhecimento

Todos os contribuidores serão listados no README.md e nas notas de release.

---

Obrigado por contribuir com o Nexus IA! 🚀