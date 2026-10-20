# TelaRec — Gravador de tela portátil para Windows 10/11

Gravador de tela gratuito, leve e sem limite artificial de duração. Grava **um monitor por vez** (qualquer um dos detectados pelo Windows) e gera vídeos **MP4 (H.264)**. Sem instalação, sem anúncios, sem telemetria, funciona offline.

> **Estado honesto do projeto (v1.0.0):** todo o código foi escrito sem acesso a um Windows nem a um compilador .NET. **Nada foi compilado nem executado ainda.** O primeiro passo é rodar o GitHub Actions; se aparecer erro de compilação, envie o log para correção. A validação com monitores reais está em [`TESTES.md`](TESTES.md).

## Como obter o executável (sem instalar nada no seu PC)

1. Crie um repositório no GitHub e envie **todo o conteúdo** desta pasta (incluindo a pasta oculta `.github`).
2. Abra a aba **Actions**. O workflow *Compilar TelaRec (Windows)* roda sozinho a cada push na branch `main` (ou clique em **Run workflow** para iniciar manualmente).
3. Ao terminar (alguns minutos), baixe `TelaRec.exe` em duas opções:
   - **Actions → execução → Artifacts → `TelaRec-win-x64`**; ou
   - **Releases** (cada execução cria uma; marcadas como *pre-release* quando vêm de `main`).
4. Para gerar uma versão numerada: crie uma tag, por exemplo `v1.0.0` (GitHub → Releases → *Draft a new release* → nova tag). O workflow publica uma Release normal.

O executável tem ~60–80 MB porque embute o .NET e o FFmpeg. Na primeira execução ele extrai o FFmpeg para `%LOCALAPPDATA%\TelaRec\bin` (sem administrador).

## Como usar

1. Execute `TelaRec.exe`.
2. Clique no monitor desejado no mapa de telas (mesma disposição das configurações de vídeo do Windows, com miniatura).
3. Escolha pasta, nome (opcional), FPS e qualidade.
4. **Iniciar gravação** → **Pausar/Retomar** → **Finalizar**. O tempo em pausa não entra no vídeo.

## Escolhas técnicas

| Item | Escolha | Motivo |
|---|---|---|
| Linguagem/UI | C# .NET 8 + WPF | Interface moderna nativa do Windows; publica em arquivo único autocontido (o usuário não instala runtime). |
| Captura | FFmpeg `ddagrab` (DXGI Desktop Duplication) | Captura **diretamente** a saída DXGI do monitor escolhido (nada de gravar tudo e recortar), eficiente e correta em qualquer DPI. |
| Mapeamento de monitor | Win32 `EnumDisplayMonitors` + DXGI `EnumAdapters1/EnumOutputs` | Liga o "Monitor N" do Windows ao par (adaptador, saída) usado pelo capturador. |
| Plano B | `gdigrab` (GDI) por coordenadas | Usado só se o DXGI falhar **e** todos os monitores estiverem em escala 100% (senão o recorte poderia errar). |
| Codificação | H.264: NVENC / Quick Sync / AMF (testados na inicialização) com fallback x264 | Compatibilidade máxima; hardware quando confiável. |
| Container | MP4 fragmentado por trecho → união sem recodificar + `faststart` | Arquivo sobrevive a quedas; pausa = novo trecho; sem manter vídeo em RAM. |
| Cor | BT.709, faixa limitada, sem escalonar | Cores corretas e texto nítido; só corta 1 px se a dimensão for ímpar (exigência do H.264). |
| Distribuição | `dotnet publish` single-file + FFmpeg embutido (gzip) | Um único `.exe`. |

Metas de tamanho (10–20 MB / 5 min) são referência; o padrão é x264 `veryfast` CRF 26 (hardware: CQ 28), 30 FPS. Os valores reais **ainda precisam ser medidos**.

## Estrutura

```
src/TelaRec.Core   lógica pura (argumentos do FFmpeg, nomes de arquivo, configurações) — testada
src/TelaRec.App    interface WPF, detecção de monitores, controle do FFmpeg
tests/TelaRec.Tests  testes unitários (xunit)
.github/workflows/build.yml   compilação, testes, executável, artefato e Release
```

## Comportamentos de segurança

- Revalida o monitor imediatamente antes de gravar; se o monitor sumir ou mudar de geometria durante a gravação, finaliza com segurança e avisa.
- Nunca sobrescreve arquivos (acrescenta ` (2)`, ` (3)`…).
- Verifica espaço livre: avisa abaixo de 3 GB e finaliza abaixo de 1 GB.
- Falha do codificador/captura: salva o que foi gravado e mostra mensagem. Se a união final falhar, os trechos ficam numa pasta oculta `.telarec-*` ao lado do destino e o caminho é informado.
- Sucesso só é informado depois que o FFmpeg fecha o MP4 final com código 0.
- A pasta temporária fica no mesmo disco do destino; a união final pode precisar temporariamente de espaço ≈ tamanho do vídeo.

## Limitações conhecidas (transparência)

- **Nunca testado em Windows real** (ver aviso no topo). O mapeamento monitor→DXGI, a opção "esconder esta janela da gravação" (`WDA_EXCLUDEFROMCAPTURE`) e telas giradas precisam de validação manual.
- Monitores girados (retrato) com `ddagrab`: comportamento não verificado.
- Sem áudio, webcam, edição, atalhos globais nem ícone de bandeja (fora do escopo da v1).
- Requer GPU/driver com Desktop Duplication (praticamente todo PC físico). Em máquinas virtuais/Área de Trabalho Remota pode cair no modo GDI.
- Windows 10 2004+ recomendado (para ocultar a janela do app da gravação).

## Licenças

O código do TelaRec é distribuído sob **GPL-3.0-or-later** (necessário por embutir o FFmpeg em build GPL). Adicione o texto completo da licença ao repositório (GitHub → *Add file → Create new file* → nome `LICENSE` → *Choose a license template* → GPL-3.0). Detalhes das dependências em [`THIRD_PARTY_LICENSES.md`](THIRD_PARTY_LICENSES.md).
