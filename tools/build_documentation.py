from pathlib import Path
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs'
LOGO = ROOT / 'assets/brand/trama-logo.png'

def base(title):
    d = Document()
    for border in list(d.styles.element.iter(qn('w:pBdr'))):
        border.getparent().remove(border)
    s = d.sections[0]
    s.page_width, s.page_height = Inches(8.5), Inches(11)
    s.top_margin, s.bottom_margin = Inches(.7), Inches(.65)
    s.left_margin = s.right_margin = Inches(.8)
    for name in ['Normal','Title','Subtitle','Heading 1','Heading 2','Heading 3']:
        st = d.styles[name]
        st.font.name = 'Calibri'
        st.font.color.rgb = RGBColor(0,0,0)
    normal=d.styles['Normal']
    normal.font.size=Pt(11)
    normal.paragraph_format.space_after=Pt(7)
    normal.paragraph_format.line_spacing=1.1
    for name,size in [('Title',30),('Heading 1',21),('Heading 2',13)]:
        st=d.styles[name]; st.font.size=Pt(size)
        st.paragraph_format.space_before=Pt(12)
        st.paragraph_format.space_after=Pt(8)
    d.core_properties.title=title
    d.core_properties.subject='Trama | Visão do produto e planejamento'
    d.core_properties.author='Projeto Trama'
    foot=s.footer.paragraphs[0]
    foot.alignment=WD_ALIGN_PARAGRAPH.RIGHT
    r=foot.add_run('TRAMA  •  ');r.font.size=Pt(9)
    field=OxmlElement('w:fldSimple');field.set(qn('w:instr'),'PAGE');foot._p.append(field)
    return d

def p(d,text): return d.add_paragraph(text)
def h(d,text): return d.add_heading(text,2)
def bullet(d,text): return d.add_paragraph(text,'List Bullet')
def page(d,title):
    d.add_page_break();d.add_heading(title,1)
def table(d,heads,rows,widths):
    t=d.add_table(rows=1,cols=len(heads)); t.autofit=False
    t.alignment=WD_TABLE_ALIGNMENT.CENTER
    for c,w in zip(t.columns,widths): c.width=Inches(w)
    for cell,text,w in zip(t.rows[0].cells,heads,widths): cell.text=text;cell.width=Inches(w)
    for row in rows:
        cells=t.add_row().cells
        for c,txt,w in zip(cells,row,widths):c.text=str(txt);c.width=Inches(w)
    for i,row in enumerate(t.rows):
        pr=row._tr.get_or_add_trPr()
        el=OxmlElement('w:cantSplit');pr.append(el)
        if i==0:
            rep=OxmlElement('w:tblHeader');pr.append(rep)
        for c in row.cells:
            c.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
            cp=c._tc.get_or_add_tcPr()
            sh=OxmlElement('w:shd');sh.set(qn('w:fill'),'17394A' if i==0 else ('F1F5F7' if i%2 else 'FFFFFF'));cp.append(sh)
            mar=OxmlElement('w:tcMar')
            for side in ['top','left','bottom','right']:
                el=OxmlElement('w:'+side);el.set(qn('w:w'),'95');el.set(qn('w:type'),'dxa');mar.append(el)
            cp.append(mar)
            for par in c.paragraphs:
                par.paragraph_format.space_after=Pt(2)
                par.paragraph_format.line_spacing=1.05
                for r in par.runs:
                    r.font.size=Pt(10)
                    if i==0:r.bold=True;r.font.color.rgb=RGBColor(255,255,255)
    borders=OxmlElement('w:tblBorders')
    for side in ['top','left','bottom','right','insideH','insideV']:
        el=OxmlElement('w:'+side);el.set(qn('w:val'),'single');el.set(qn('w:sz'),'4');el.set(qn('w:color'),'D9D9D9');borders.append(el)
    t._tbl.tblPr.append(borders)
    p(d,'')
    return t

def stage(d,title,goal,delivery,done):
    h(d,title);p(d,goal)
    p(d,'Entregas: '+delivery)
    p(d,'Concluída quando: '+done)

def main_document():
    d=base('Trama documentação do produto')
    if LOGO.exists():d.add_picture(str(LOGO),width=Inches(3.7))
    d.add_heading('Trama documentação do produto',0)
    p(d,'Visão funcional e técnica com plano de desenvolvimento')
    p(d,'Versão 1.0  |  27 de outubro de 2026  |  Uso pessoal e execução local')
    h(d,'Propósito')
    p(d,'Construir um ambiente pessoal que conecte ideias, anotações, códigos, prints, estudos, projetos, rotina e finanças. Um assistente por texto e voz permitirá consultar informações e executar ações controladas no aplicativo e no computador.')
    p(d,'O problema inicial é a dificuldade de organizar e recuperar conteúdo disperso no Notepad e no Notion, incluindo limitações de recursos e planos pagos. A prioridade é oferecer captura rápida, organização flexível, busca e domínio sobre os próprios dados.')
    h(d,'Direção estabelecida')
    bullet(d,'Nome escolhido: Trama. Aplicativo inicialmente local, para uso individual.')
    bullet(d,'Identidade futurista e conversa inspirada na experiência de um assistente como Jarvis, com identidade visual própria.')
    bullet(d,'Estudos com diferentes conteúdos e tipos de anotação; preparação para apoiar pesquisa e escrita científica.')
    bullet(d,'Evolução gradual, com uma base útil antes de ampliar automações e integrações.')
    h(d,'Como usar este documento')
    p(d,'As necessidades relatadas orientam o escopo. Tecnologias, etapas detalhadas e critérios de aceite abaixo constituem a proposta de implementação. Atualizar este documento a cada decisão importante; registrar novas ideias no backlog antes de adicioná-las à versão em desenvolvimento.')
    p(d,'Responsável pelo produto: a definir. As etapas representam ordem e dependências, sem compromisso de datas ou estimativas de duração.')

    page(d,'1 Visão do produto e módulos')
    p(d,'O Trama deve reduzir o esforço entre capturar uma informação, usá-la em um projeto e transformá-la em uma ação. A navegação parte do painel do dia e oferece acesso à caixa de entrada, conhecimento, projetos, agenda e assistente.')
    table(d,['Módulo','Conteúdo principal'],[
        ('Conhecimento','Ideias, notas, código, prints, links, tags e relações entre registros.'),
        ('Projetos','Objetivos, tarefas, referências, notas e histórico de desenvolvimento.'),
        ('Estudos e pesquisa','Áreas, assuntos, fichamentos, exercícios, experimentos e fontes.'),
        ('Agenda e rotina','Compromissos, blocos de horário, cronogramas e recorrências.'),
        ('Diário de atividades','Registro diário do trabalho e estudo efetivamente realizados.'),
        ('Painéis','Visão diária e retrospectiva semanal do planejamento e da execução.'),
        ('Finanças','Receitas, contas, vencimentos, recorrências e pagamentos.'),
        ('Cofre','Senhas, tokens, credenciais e códigos sensíveis protegidos.'),
        ('Assistente','Conversa, consultas, ações no Trama e abertura de aplicativos.')
    ],[1.55,5.35])
    h(d,'Exemplo de uso integrado')
    p(d,'Durante um estudo de C#, o usuário cola um print, salva um trecho de código, relaciona ambos a um projeto e registra uma hora de atividade. A tarefa planejada permanece no calendário; a execução aparece no dia em que ocorreu. Ao fim da semana, o painel reúne o progresso e as pendências.')
    h(d,'Limites iniciais')
    p(d,'A primeira entrega é individual e local. Colaboração, sincronização entre dispositivos, aplicativos móveis, integrações bancárias e automação irrestrita do sistema ficam para avaliação futura. A base de notas e agenda precisa funcionar sem um modelo de IA carregado.')

    page(d,'2 Organização básica e conhecimento')
    h(d,'Captura e edição')
    p(d,'A caixa de entrada aceita anotações rápidas, sem classificação obrigatória. O editor combina texto formatado, links, listas, blocos de código com linguagem identificada e imagens coladas ou anexadas. Permitir salvar rascunhos e indicar claramente o estado de gravação.')
    h(d,'Estrutura flexível')
    bullet(d,'Áreas representam responsabilidades contínuas, como trabalho, estudos e vida pessoal.')
    bullet(d,'Projetos possuem um objetivo e podem reunir tarefas, notas, arquivos e referências.')
    bullet(d,'Tags atravessam áreas e projetos. Links relacionam conceitos sem duplicar o conteúdo.')
    bullet(d,'Arquivar preserva o histórico. Exclusão deve oferecer recuperação por lixeira, conforme a política definida.')
    h(d,'Busca e recuperação')
    p(d,'Buscar em títulos e textos, com filtros de tipo, tag, projeto, assunto e período. A busca textual vem primeiro. OCR de prints e busca semântica são evoluções: anexar uma imagem não implica que seu texto já seja pesquisável.')
    h(d,'Registros propostos')
    table(d,['Registro','Campos de referência'],[
        ('Nota','Identificador, título, corpo, tipo, criação, alteração e arquivamento.'),
        ('Código','Conteúdo, linguagem, descrição, contexto e vínculo com projeto.'),
        ('Anexo','Nome, tipo, tamanho, caminho gerenciado e vínculo com registro.'),
        ('Projeto','Nome, objetivo, estado, área e datas relevantes.'),
        ('Tag e relação','Rótulo reutilizável; origem, destino e tipo de relação.')
    ],[1.35,5.55])
    h(d,'Preservação dos dados')
    p(d,'O aplicativo deve gerenciar a cópia dos anexos para sua própria pasta de dados. O backup reúne banco, anexos e versão do formato. Exportações legíveis, como Markdown para notas e arquivos originais para imagens, reduzem dependência do aplicativo. Importação de arquivos existentes será avaliada após estabilizar o modelo de dados.')

    page(d,'3 Estudos e produção científica')
    p(d,'Assunto e tipo de anotação são dimensões independentes. O mesmo assunto pode conter resumo, exercício, conceito, ficha de leitura, dúvida e experimento. Evitar obrigar o usuário a reproduzir a mesma hierarquia de pastas para cada tipo.')
    table(d,['Dimensão','Exemplos'],[
        ('Área de estudo','Computação, matemática, idiomas.'),
        ('Assunto','Engenharia de software, algoritmos, estatística.'),
        ('Tipo de conteúdo','Resumo, exercício, conceito, fichamento, experimento, dúvida.'),
        ('Fonte','Livro, artigo científico, curso, aula, documentação.'),
        ('Relação','Projeto, conceito relacionado, pré-requisito e tarefa de revisão.')
    ],[1.5,5.4])
    h(d,'Modelos de anotação')
    bullet(d,'Conceito: definição, explicação própria, exemplo e referências.')
    bullet(d,'Exercício: enunciado, tentativa, solução, dificuldade e aprendizado.')
    bullet(d,'Ficha de leitura: referência, objetivo do texto, síntese, citações com página e comentários próprios.')
    bullet(d,'Experimento: pergunta, hipótese quando aplicável, método, ambiente, dados, resultado e limitações.')
    h(d,'Apoio a artigos científicos')
    p(d,'Uma pesquisa pode reunir problema, pergunta, objetivos, revisão da literatura, método, evidências, resultados e limitações. Distinguir citação direta, paráfrase e interpretação própria. Cada afirmação baseada em uma fonte deve manter um vínculo verificável com a referência e, quando necessário, com a página ou seção.')
    p(d,'Prever campos bibliográficos como autoria, título, ano, DOI ou URL e data de acesso quando aplicável. Avaliar importação e exportação BibTeX e RIS antes de criar um gerenciador bibliográfico completo. O documento científico final poderá ser preparado em Word ou outra ferramenta de escrita.')
    p(d,'A IA pode auxiliar na organização e em rascunhos, mas não deve inventar referências, evidências ou resultados. A qualidade científica depende do método e da conferência das fontes. O próprio Trama poderá apoiar uma pesquisa sobre captura por voz e recuperação de conhecimento, caso exista pergunta e avaliação adequadas.')

    page(d,'4 Rotina painéis e finanças')
    h(d,'Agenda e execução diária')
    p(d,'Planejar tarefas de trabalho e estudo em datas e blocos de horário, incluindo recorrências e lembretes. Registrar separadamente o realizado: data, duração, observações e vínculos com projeto ou assunto. Reagendar uma tarefa não deve deslocar retroativamente seu histórico de execução.')
    p(d,'Compromissos possuem horário; tarefas podem ter apenas prazo; sessões de atividade representam execução. Configurar início da semana, fuso horário e antecedência dos lembretes. Informar se um lembrete depende de o aplicativo estar em execução.')
    h(d,'Painel diário e retrospectiva semanal')
    bullet(d,'Hoje: compromissos, prioridades, tarefas pendentes e próximos vencimentos.')
    bullet(d,'Semana: planejado versus realizado, horas por área e tarefas concluídas ou transferidas.')
    bullet(d,'Estudos: assuntos trabalhados, sessões e dúvidas a revisar.')
    bullet(d,'Projetos: progresso, próximas ações e histórico de trabalho.')
    p(d,'Usar cores distintas para trabalho, estudos e vida pessoal. Estados como pendente, concluído ou atrasado também precisam de texto ou ícone. Definir como tratar tarefas canceladas e sem duração para não distorcer os indicadores.')
    h(d,'Controle financeiro pessoal')
    p(d,'Cadastrar receitas e despesas previstas e realizadas, categorias, vencimentos e recorrências. Registrar pagamento separadamente da previsão; gerar a próxima ocorrência sem sobrescrever períodos anteriores. Contas futuras devem aparecer no painel e nos lembretes.')
    p(d,'Os valores monetários usam representação decimal e moeda explícita. Não misturar totais de moedas diferentes. Na primeira versão financeira, o lançamento é manual; integração bancária e movimentação de dinheiro não fazem parte da entrega.')
    h(d,'Critérios funcionais centrais')
    p(d,'A visualização deve permitir identificar o que foi planejado, o que aconteceu e o que exige atenção. Marcar uma conta como paga deve atualizar os totais realizados sem apagar a previsão original. Resumos de produtividade precisam indicar o período e os filtros usados.')

    page(d,'5 Assistente por voz e resumo do dia')
    p(d,'A visão é abrir o Trama e conversar naturalmente com um assistente que conhece as informações permitidas do usuário. Ele apresenta agenda e pendências, consulta clima e notícias quando configurados e executa ações delimitadas.')
    h(d,'Fluxo de uma interação')
    p(d,'Microfone → transcrição → interpretação → consulta ou ação proposta → validação pelo aplicativo → execução → resposta por texto e voz. Exibir a transcrição e permitir correção; oferecer interação por texto quando o microfone ou o modelo estiver indisponível.')
    h(d,'Primeiros comandos')
    bullet(d,'“Guarde essa ideia no projeto Trama.”')
    bullet(d,'“Registre que hoje estudei C# por uma hora.”')
    bullet(d,'“Quais tarefas ficaram pendentes nesta semana?”')
    bullet(d,'“Abra o VS Code e mostre as tarefas do projeto.”')
    h(d,'Atualizações ao abrir')
    p(d,'O resumo combina data, agenda, prioridades, vencimentos e progresso recente. Clima depende de uma localização configurada e de uma fonte externa. Notícias e assuntos em destaque devem mostrar fonte, link e horário de atualização; definir temas e região com o usuário, pois destaque não equivale a um ranking universal.')
    p(d,'Dados externos não podem ser apresentados como atuais quando estiverem desatualizados. Sem internet, apresentar informações locais e sinalizar a indisponibilidade das consultas externas. O usuário escolhe se a abertura inicia uma saudação falada ou apenas exibe o painel.')
    h(d,'Ações no computador e evolução')
    p(d,'Abrir programas por um catálogo de nomes e caminhos cadastrados. O modelo não recebe execução arbitrária de comandos. Ações de maior impacto, como apagar arquivos ou enviar mensagens, exigem permissões e confirmação apropriadas, caso sejam implementadas.')
    p(d,'Começar com pressionar para falar. Palavra de ativação, escuta contínua, interrupção da fala, memória de preferências e sugestões proativas ficam para etapas posteriores. A IA não deve registrar uma ação como concluída antes de receber o resultado da execução.')

    page(d,'6 Identidade visual e experiência')
    h(d,'Direção da marca')
    p(d,'A marca Trama representa conexões entre ideias, conhecimentos e ações. A logo utiliza uma composição geométrica de fios entrelaçados e um nome legível. O resultado é uma proposta visual inicial, sujeita à aprovação e aos refinamentos de uso em tamanhos pequenos.')
    h(d,'Interface futurista')
    p(d,'Base escura em grafite, acentos em ciano e teal, profundidade discreta e animações pontuais. A inspiração em assistentes de ficção orienta a experiência; a identidade deve ser própria. Não reproduzir símbolos ou elementos de marcas de terceiros.')
    table(d,['Papel','Referência inicial'],[
        ('Fundo principal','Grafite #101820.'),
        ('Superfície de painéis','Azul grafite #17394A.'),
        ('Destaques','Ciano #25D9E8 e teal #18B8A6.'),
        ('Texto em fundo escuro','Branco suave #EFF5F7.'),
        ('Áreas e estados','Paleta adicional configurável, sempre com rótulo ou ícone.')
    ],[2.1,4.8])
    h(d,'Estados visíveis do assistente')
    p(d,'Aguardando, ouvindo, processando, respondendo e aguardando autorização. A animação deve informar o estado real. Disponibilizar cancelamento da escuta e interrupção da resposta, com indicação clara de uso do microfone.')
    h(d,'Leitura e acessibilidade')
    bullet(d,'Editor de estudos e código com tipografia confortável e espaço para imagens.')
    bullet(d,'Atalhos de teclado, foco visível e navegação previsível.')
    bullet(d,'Contraste verificado, zoom de conteúdo e intensidade de animação configurável.')
    bullet(d,'Textos e ícones que complementam as cores de categorias e estados.')
    p(d,'A logo raster entregue pode atender à documentação e à exploração inicial. Antes de consolidar a identidade do produto, validar versões vetorial, monocromática e de ícone, além da disponibilidade de marca.')

    page(d,'7 Arquitetura e tecnologias propostas')
    p(d,'Adotar um aplicativo modular, em um único produto, com separação entre interface, casos de uso, domínio e infraestrutura. A arquitetura permite substituir reconhecimento de voz, modelo de linguagem e fontes externas sem reescrever as regras de negócio.')
    table(d,['Componente','Proposta e finalidade'],[
        ('Desktop','C# com Avalonia e MVVM para a interface; começar validando o Windows.'),
        ('Domínio e aplicação','Bibliotecas C# para entidades, regras, consultas e ações.'),
        ('Persistência','SQLite com Entity Framework Core e migrações versionadas.'),
        ('Anexos','Pasta gerenciada; banco mantém referências e metadados.'),
        ('Modelo de linguagem','Ollama com modelo local compatível com o hardware e a licença.'),
        ('Transcrição','whisper.cpp como candidato, mediante teste em português.'),
        ('Síntese de voz','Provedor substituível; selecionar após teste de voz e licença.'),
        ('Integrações','Adaptadores para aplicativos, notificações, clima e notícias.')
    ],[1.55,5.35])
    h(d,'Organização sugerida do código')
    p(d,'Trama.Desktop contém telas e apresentação. Trama.Application expõe os casos de uso. Trama.Domain define as regras. Trama.Infrastructure implementa persistência e arquivos. Trama.Assistant integra voz e modelos. Adaptadores do sistema operacional ficam isolados. Projetos de teste verificam os comportamentos críticos.')
    h(d,'Decisões técnicas para o início')
    p(d,'Usar uma versão .NET LTS compatível com os componentes selecionados, fixando as versões na implementação. Node e Python ficam opcionais, para necessidades comprovadas. Não é necessário manter um servidor web para as regras locais; o runtime de IA pode operar como processo separado.')
    p(d,'A versão gratuita dos componentes e a ausência de cobrança por chamada não eliminam consumo de hardware, custos opcionais de serviços e condições de licença. Não escolher o modelo final antes de verificar RAM, CPU, GPU e latência no computador de destino.')

    page(d,'8 Segurança privacidade e continuidade')
    h(d,'Separação do cofre')
    p(d,'Credenciais, tokens e códigos sensíveis pertencem a um domínio protegido. Anotações comuns podem conter exemplos de código; segredos reais não devem entrar automaticamente na busca geral, nos logs, nas exportações comuns ou no contexto do assistente.')
    p(d,'SQLite comum não criptografa os dados automaticamente. A implementação do cofre precisa definir criptografia autenticada, gestão de chaves, derivação de chave quando houver senha mestra, bloqueio por inatividade e comportamento da área de transferência. Utilizar bibliotecas consolidadas; não criar um algoritmo próprio.')
    h(d,'Permissões do assistente')
    bullet(d,'Expor apenas ações cadastradas, com argumentos validados e escopo definido.')
    bullet(d,'Tratar notas, páginas e notícias como dados; seu conteúdo não concede permissões ao modelo.')
    bullet(d,'Registrar o resultado das ações sem armazenar senhas ou outros dados sensíveis.')
    bullet(d,'Configurar explicitamente qualquer envio a um serviço externo. O modo local deve evitar provedores de nuvem para a conversa.')
    h(d,'Backup e restauração')
    p(d,'O backup precisa produzir uma cópia consistente do banco e incluir anexos, configurações relevantes e versão do formato. Definir destino e retenção. Quando houver cofre, proteger o backup e explicar quais chaves ou senhas são necessárias à recuperação.')
    p(d,'Validar integridade antes da restauração e oferecer proteção dos dados atuais. Testar recuperação em um perfil limpo, inclusive sem os arquivos originais. Backup que nunca foi restaurado não comprova recuperação.')
    h(d,'Falhas e manutenção')
    p(d,'Planejar migrações com proteção dos dados, mensagens de erro úteis e logs sem conteúdo sensível. Evitar gravações parciais entre registros e anexos. Definir política de lixeira e exclusão. Documentar limitações dos lembretes com o aplicativo fechado e do assistente com hardware insuficiente.')

    page(d,'9 Etapas da fundação ao assistente')
    p(d,'A sequência preserva as etapas de organização básica, rotina, marco de voz e expansão. Estudos detalhados entram na organização; finanças e cofre recebem entregas próprias por exigirem regras e validações distintas.')
    stage(d,'Etapa 0 Fundação e protótipos','Objetivo: validar as decisões que afetam toda a aplicação.',
          'Estrutura da solução, repositório, identidade inicial, protótipo do editor com prints e código, banco inicial e teste de IA no hardware disponível.',
          'o aplicativo abrir no Windows, salvar e reabrir um registro de teste e demonstrar a viabilidade das tecnologias. Registrar decisões e limitações.')
    stage(d,'Etapa 1 Organização básica','Objetivo: substituir a captura dispersa por um acervo recuperável.',
          'Notas, código, prints, projetos, tags, busca textual, caixa de entrada, exportação e backup com restauração. Tipos de anotação e assuntos de estudo.',
          'for possível criar, editar, localizar e relacionar conteúdos; fechar e reabrir sem perda; restaurar banco e anexos em um perfil limpo.')
    stage(d,'Etapa 2 Rotina e visão semanal','Objetivo: conectar planejamento ao histórico real de trabalho e estudo.',
          'Agenda semanal, horários, recorrências, lembretes, registro diário, cores por área e painel de planejado versus realizado.',
          'reagendar não alterar a execução passada, recorrências não duplicarem indevidamente e os totais do painel corresponderem aos registros do período.')
    stage(d,'Etapa 3 Marco de voz','Objetivo: consultar e alimentar o Trama por conversa.',
          'Chat local, botão de voz, transcrição corrigível, resposta falada, consulta a notas e agenda, criação de notas e tarefas e abertura de aplicativos cadastrados.',
          'o usuário concluir esses fluxos em português, cancelar a interação e receber erros claros; a aplicação funcionar por texto se a voz falhar; a IA respeitar o catálogo de ações.')

    page(d,'10 Etapas de expansão')
    stage(d,'Etapa 4 Resumo do dia e fontes externas','Objetivo: apresentar um início de dia útil e contextualizado.',
          'Resumo de agenda e pendências, clima por localização configurada, notícias por temas e região, fontes e horários de atualização, saudação configurável.',
          'o resumo refletir os dados locais, falhas de rede não bloquearem o aplicativo e conteúdo externo indicar fonte e atualidade.')
    stage(d,'Etapa 5 Finanças pessoais','Objetivo: acompanhar receitas e contas previstas e realizadas.',
          'Lançamentos manuais, categorias, recorrências, vencimentos, confirmação de pagamento e painel financeiro.',
          'os totais respeitarem moeda e período, pagar uma ocorrência preservar o histórico e lembretes refletirem o estado atual da conta.')
    stage(d,'Etapa 6 Cofre de credenciais','Objetivo: guardar segredos com controles próprios de proteção.',
          'Criptografia, desbloqueio, bloqueio automático, gestão de chaves, proteção de cópias e backup recuperável. Revisão da implementação antes de uso real.',
          'segredos não aparecerem nos logs ou na busca geral, bloqueio for efetivo e restauração protegida funcionar com as credenciais de recuperação esperadas.')
    stage(d,'Etapa 7 Pesquisa e assistência avançada','Objetivo: ampliar estudos, escrita e continuidade da conversa.',
          'Fichamentos bibliográficos, BibTeX/RIS, vínculos entre afirmações e fontes; avaliar OCR, busca semântica, palavra de ativação, interrupção da fala e preferências.',
          'cada recurso aprovado possuir critérios próprios, resultados verificáveis e avaliação de privacidade, licença e desempenho.')
    p(d,'As etapas 5 e 6 dependem da fundação e da organização básica; sua ordem pode mudar conforme a prioridade do usuário. Escuta contínua e sincronização permanecem opcionais. Nenhuma etapa deve impedir a correção de falhas de persistência ou segurança anteriores.')

    page(d,'11 Validação e valor de engenharia')
    h(d,'Fluxos que precisam ser demonstráveis')
    table(d,['Cenário','Resultado esperado'],[
        ('Captura e recuperação','Criar nota com código e print, reiniciar e localizar pela busca.'),
        ('Backup completo','Restaurar em perfil limpo, abrindo notas e imagens sem depender da origem.'),
        ('Histórico da semana','Planejar na terça e executar na quinta, preservando as duas datas.'),
        ('Comando de voz','Exibir interpretação e criar uma única tarefa após validação.'),
        ('Falha de IA ou rede','Manter notas e agenda acessíveis e informar indisponibilidade.'),
        ('Controle do assistente','Recusar ações fora do catálogo e não expor credenciais.'),
        ('Finanças','Separar previsão e pagamento sem erros de arredondamento.'),
        ('Migração de dados','Atualizar o esquema preservando conteúdo e vínculos existentes.')
    ],[1.55,5.35])
    h(d,'Estratégia de qualidade')
    p(d,'Testar regras de recorrência, valores monetários, permissões e integridade. Usar testes de integração para banco, anexos, migrações e recuperação. Conferir visualmente editor, calendário, estados do assistente e navegação por teclado. Avaliar voz com frases reais em português, medindo latência e erros antes de definir metas.')
    h(d,'Portfólio para engenharia de software')
    p(d,'O projeto pode demonstrar levantamento de requisitos, arquitetura modular, modelagem, segurança, integração com IA e entrega de software. Manter README, instruções de instalação, decisões de arquitetura, testes relevantes, integração contínua e notas de versão. Uma demonstração deve explicar o problema, a escolha técnica, o resultado e as limitações.')
    p(d,'O valor profissional depende da qualidade e da capacidade de justificar as decisões. Para pesquisa científica, estabelecer uma pergunta, um método e uma avaliação; desenvolver o produto por si só não constitui um resultado científico.')

    page(d,'12 Decisões pendentes e referências')
    table(d,['Definição','Situação inicial'],[
        ('Nome e execução','Trama escolhido; uso inicialmente local e individual.'),
        ('Plataformas','Windows como alvo proposto; confirmar necessidade futura de Linux ou macOS.'),
        ('Stack principal','C#, Avalonia e SQLite propostos; validar no protótipo.'),
        ('Hardware e IA','RAM, CPU e GPU a levantar; modelo e síntese de voz a selecionar.'),
        ('Editor','Componente e formato de armazenamento a testar com código e prints.'),
        ('Fontes externas','Localização, fontes de clima, temas e fontes de notícias a definir.'),
        ('Cofre','Estratégia de chaves, recuperação e biblioteca criptográfica a definir.'),
        ('Lembretes e backup','Execução em segundo plano, destino e retenção a definir.')
    ],[1.65,5.25])
    h(d,'Controle de evolução')
    p(d,'Versão 1.0, de 27 de outubro de 2026: consolidação da visão, nome, módulos, estudos, pesquisa, interface futurista, assistência por voz, integrações e plano de desenvolvimento. Utilizar o modelo de documentação complementar para propostas, requisitos, critérios de aceite e decisões futuras.')
    h(d,'Referências técnicas para implementação')
    for text in [
        'Avalonia e plataformas: https://docs.avaloniaui.net/docs/supported-platforms',
        'Entity Framework Core com SQLite: https://learn.microsoft.com/en-us/ef/core/providers/sqlite/',
        'Ollama e configurações de execução local: https://github.com/ollama/ollama/blob/main/docs/faq.mdx',
        'Reconhecimento de voz whisper.cpp: https://github.com/ggml-org/whisper.cpp',
        'Alternativa WPF com Blazor Hybrid: https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/tutorials/',
        'Alternativa Electron: https://www.electronjs.org/docs/latest'
    ]:p(d,text)
    p(d,'Estas referências apoiam a avaliação técnica. Conferir compatibilidade de versões, licenças e requisitos no momento da implementação.')
    d.save(OUT/'Trama_Documentacao_do_Produto_v1.docx')

def template_document():
    d=base('Trama modelo de documentação')
    if LOGO.exists():d.add_picture(str(LOGO),width=Inches(2.6))
    d.add_heading('Trama modelo de documentação',0)
    p(d,'Modelo editável para funcionalidades módulos e decisões')
    p(d,'Duplique este arquivo para cada proposta ou entrega. Substitua os campos entre colchetes, mantenha identificadores estáveis e vincule os requisitos à etapa correspondente do documento do produto.')
    h(d,'Identificação')
    for text in ['Título da proposta: [preencher]','Identificador: [TRM-000]   Versão: [0.1]   Data: [dd/mm/aaaa]',
                 'Responsável: [preencher]   Módulo: [preencher]   Etapa: [0 a 7]',
                 'Situação: [ideia / em análise / aprovada / em desenvolvimento / validada / adiada]']:
        p(d,text)
    h(d,'Problema e resultado esperado')
    p(d,'[Descreva quem enfrenta o problema, quando ele acontece e o resultado observável esperado. Inclua um exemplo concreto.]')
    h(d,'Escopo da entrega')
    p(d,'Incluído: [comportamentos que serão entregues nesta versão].')
    p(d,'Fora desta entrega: [limites e ideias reservadas para depois].')
    p(d,'Dependências: [etapas, componentes, dados ou decisões necessárias].')
    h(d,'Fluxo principal')
    for text in ['1. [Ação inicial do usuário]','2. [Resposta do sistema e validações]','3. [Resultado e informação apresentada]']:
        p(d,text)
    p(d,'Falhas e alternativas: [cancelamento, indisponibilidade, dados inválidos e recuperação].')

    page(d,'Requisitos dados e critérios de aceite')
    h(d,'Requisitos verificáveis')
    table(d,['ID','Comportamento esperado','Prioridade'],[
        ('RF-001','[O sistema deve permitir...]','[essencial]'),
        ('RF-002','[O sistema deve preservar...]','[desejável]'),
        ('RNF-001','[Meta mensurável e condição de avaliação]','[essencial]')
    ],[.9,4.8,1.2])
    h(d,'Dados e vínculos')
    p(d,'Entidades e campos: [nome, tipo, obrigatoriedade e valores permitidos].')
    p(d,'Relações: [projeto, assunto, tarefa, referência, anexo ou outro registro].')
    p(d,'Persistência e migração: [o que muda, como preservar dados existentes].')
    p(d,'Privacidade e permissões: [dados sensíveis, acesso do assistente e envio externo].')
    p(d,'Backup e exportação: [o que deve ser recuperável e em qual formato].')
    h(d,'Critérios de aceite')
    p(d,'CA-001 — Dado [contexto], quando [ação], então [resultado verificável].')
    p(d,'CA-002 — Dado [falha ou indisponibilidade], quando [ação], então [comportamento de recuperação].')
    p(d,'CA-003 — Dado [dados existentes], quando [reinício, atualização ou restauração], então [integridade esperada].')
    h(d,'Interface e acessibilidade')
    p(d,'Telas e estados: [vazio, carregando, sucesso, erro, confirmação].')
    p(d,'Referências visuais: [anexar print ou descrever layout].')
    p(d,'Teclado, contraste e animações: [definir comportamentos e verificação].')

    page(d,'Decisões validação e histórico')
    h(d,'Registro de decisão técnica')
    p(d,'ADR-000 — [Título da decisão]')
    p(d,'Contexto: [restrições e motivo para decidir agora].')
    p(d,'Alternativas: [opções consideradas e seus impactos].')
    p(d,'Decisão: [opção escolhida e justificativa].')
    p(d,'Consequências: [benefícios, custos, riscos e limitações].')
    p(d,'Situação e revisão: [proposta / aceita / substituída; condição para reavaliar].')
    h(d,'Plano de validação')
    table(d,['Cenário','Resultado esperado','Evidência'],[
        ('[Fluxo principal]','[Resultado observável]','[teste ou demonstração]'),
        ('[Falha relevante]','[Recuperação esperada]','[registro da verificação]'),
        ('[Persistência]','[Integridade esperada]','[teste de restauração]')
    ],[1.65,2.8,2.45])
    h(d,'Conclusão da entrega')
    p(d,'[Registrar requisitos atendidos, verificações executadas, limitações conhecidas e versão entregue. Não marcar como concluída sem comprovar os critérios de aceite.]')
    h(d,'Backlog e questões abertas')
    p(d,'[Listar novas ideias e decisões pendentes com prioridade, responsável e dependências. Datas somente quando acordadas.]')
    h(d,'Referências e histórico')
    p(d,'Fontes: [título, autor ou organização, URL ou DOI, versão e data de acesso quando aplicável].')
    p(d,'Histórico: [data | versão | alteração | responsável].')
    d.save(OUT/'Trama_Modelo_de_Documentacao.docx')

if __name__=='__main__':
    OUT.mkdir(exist_ok=True)
    main_document();template_document()
    print('Documentos criados em',OUT)
