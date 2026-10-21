# Testes e validação

## Automatizados (rodam no GitHub Actions, etapa "Testes unitários")
Cobrem apenas lógica pura: argumentos do FFmpeg (índices DXGI, coordenadas negativas, sem escalonamento, MP4 fragmentado), nomes de arquivo sem sobrescrita, formatação de tempo, configurações e comparação de geometria de monitor. A etapa é *informativa* (não bloqueia o executável). **Compilar com sucesso não prova que a gravação funciona.**

## Manuais (nenhum foi executado ainda)
Marque conforme for validando. Para cada item anote Windows, GPU e resultado.

| # | Teste | Resultado |
|---|---|---|
| 1 | Abrir o .exe em Windows 10 e em Windows 11 limpos (sem ferramentas de desenvolvimento) | ☐ |
| 2 | 1 monitor: aparece "Monitor 1" com resolução correta | ☐ |
| 3 | 2 monitores: ambos aparecem, disposição igual às configurações do Windows | ☐ |
| 4 | 3+ monitores: todos listados | ☐ |
| 5 | Gravar o Monitor 2 com 3 telas: vídeo contém **só** o Monitor 2 | ☐ |
| 6 | Monitores com resoluções diferentes | ☐ |
| 7 | Monitor à esquerda/acima do principal (coordenadas negativas) | ☐ |
| 8 | Escalas de DPI diferentes (100%/125%/150%) em monitores distintos | ☐ |
| 9 | Monitor girado (retrato) | ☐ |
| 10 | Iniciar → pausar → retomar → finalizar; duração do vídeo ≈ tempo ativo (sem o tempo pausado) | ☐ |
| 11 | MP4 abre em Windows Media Player/Filmes e TV, VLC e navegador; permite avançar/retroceder | ☐ |
| 12 | Resolução e proporção do vídeo = do monitor (ver propriedades; ímpar perde no máx. 1 px) | ☐ |
| 13 | Texto pequeno legível nas 3 qualidades; medir MB em 5 e 10 min (meta 10–20 / 20–40 MB) | ☐ |
| 14 | Gravação ≥ 2 h: memória estável, CPU baixa, arquivo íntegro | ☐ |
| 15 | Disco quase cheio: aviso aos 3 GB, finalização aos 1 GB, arquivo reproduzível | ☐ |
| 16 | Pasta sem permissão / pasta inexistente: mensagem clara | ☐ |
| 17 | Desconectar o monitor gravado durante a gravação: finaliza com segurança e avisa | ☐ |
| 18 | Alterar resolução do monitor gravado durante a gravação | ☐ |
| 19 | Matar o ffmpeg.exe pelo Gerenciador de Tarefas durante a gravação: mensagem e trecho salvo | ☐ |
| 20 | Fechar a janela durante a gravação: pergunta e salva | ☐ |
| 21 | Janela do TelaRec **não** aparece no vídeo (opção ativada) | ☐ |
| 22 | NVIDIA / Intel / AMD: encoder de hardware detectado e vídeo válido; PC sem GPU: cai para x264 | ☐ |
| 23 | Sem internet: tudo funciona; nenhum tráfego de rede do app | ☐ |
| 24 | Após fechar, nenhum `ffmpeg.exe` ou `TelaRec.exe` fica no Gerenciador de Tarefas | ☐ |
| 25 | Nome de arquivo repetido não sobrescreve | ☐ |
| 26 | Efeito de clique: círculo AZUL no clique esquerdo e LARANJA no direito, no ponto exato, visíveis no vídeo gravado | ☐ |
| 27 | Efeito de clique em monitor secundário, com escala de DPI ≠ 100% e com coordenadas negativas: círculo no lugar certo | ☐ |
| 28 | Sons diferentes para esquerdo/direito; sem efeito durante pausa; clique em outro monitor não gera efeito | ☐ |
| 29 | Efeito de clique desligado: nenhum hook/janela extra; mouse sem atraso perceptível com o efeito ligado | ☐ |
