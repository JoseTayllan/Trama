"""Generate the Portuguese PDF distributed to Windows testers. Requires reportlab."""

from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.utils import ImageReader
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, PageBreak, KeepTogether


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "output/pdf/Trama_Guia_de_Execucao_Windows.pdf"
INK = colors.HexColor("#162838")
MUTED = colors.HexColor("#4B6070")
TEAL = colors.HexColor("#087D7A")
LIGHT = colors.HexColor("#EAF5F3")


def register_fonts():
    font_dir = Path("C:/Windows/Fonts")
    candidates = [
        (font_dir / "segoeui.ttf", font_dir / "segoeuib.ttf"),
        (Path("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"),
         Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf")),
    ]
    for regular, bold in candidates:
        if regular.exists() and bold.exists():
            pdfmetrics.registerFont(TTFont("Guide", str(regular)))
            pdfmetrics.registerFont(TTFont("GuideBold", str(bold)))
            pdfmetrics.registerFontFamily("Guide", normal="Guide", bold="GuideBold")
            return
    raise RuntimeError("Install Segoe UI or DejaVu Sans to generate the guide.")


def build():
    register_fonts()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    body = ParagraphStyle("Body", fontName="Guide", fontSize=10.4, leading=15.5,
                          textColor=INK, spaceAfter=9, alignment=TA_LEFT)
    title = ParagraphStyle("Title", parent=body, fontName="GuideBold", fontSize=25,
                           leading=30, spaceAfter=9)
    subtitle = ParagraphStyle("Subtitle", parent=body, fontSize=11, leading=16,
                              textColor=MUTED, spaceAfter=17)
    heading = ParagraphStyle("Heading", parent=body, fontName="GuideBold", fontSize=12.5,
                             leading=17, textColor=TEAL, spaceBefore=10, spaceAfter=7)
    note = ParagraphStyle("Note", parent=body, backColor=LIGHT, borderPadding=10,
                          fontSize=10, leading=15, spaceBefore=6, spaceAfter=15)
    small = ParagraphStyle("Small", parent=body, fontSize=8, leading=11, textColor=MUTED)
    story = []

    def p(text, style=body):
        return Paragraph(text, style)

    def step(number, label, text):
        story.append(KeepTogether([p(f"{number:02d}  {label}", heading), p(text)]))

    story.extend([p("Guia para testar o Trama", title),
                  p("Windows x64 | Extração, abertura e atalho<br/>Versão do guia: 29/10/2026", subtitle),
                  p("Você receberá um arquivo ZIP com o aplicativo. Não precisa instalar .NET, Ubuntu, WSL ou ferramentas de programação. Use um computador Windows de 64 bits (x64).", note)])
    step(1, "Baixe e extraia todos os arquivos",
         "Salve o ZIP recebido. Clique nele com o botão direito e escolha <b>Extrair Tudo...</b>. "
         "Selecione uma pasta e conclua a extração. <b>Execute sempre a cópia extraída</b>, sem abrir o programa de dentro do ZIP.")
    step(2, "Escolha um local fixo para a pasta",
         "Você pode deixar a pasta extraída em <b>Documentos</b> ou na <b>Área de Trabalho</b>. "
         "Abra a pasta até encontrar <b>Trama.Desktop.exe</b>, junto de várias DLLs e outros arquivos. "
         "Mantenha tudo junto: mover somente o exe impede o funcionamento.")
    step(3, "Abra o Trama",
         "Dê dois cliques em <b>Trama.Desktop.exe</b>. Se o Windows ocultar as extensões, ele pode aparecer como "
         "<b>Trama.Desktop</b>, do tipo <b>Aplicativo</b>. Aguarde a janela e confira o estado "
         "<b>Pronto · armazenamento local</b>. Não é necessário executar como administrador.")
    step(4, "Crie o atalho na Área de Trabalho",
         "Clique com o botão direito em <b>Trama.Desktop.exe</b>. No Windows 11, selecione "
         "<b>Mostrar mais opções</b>, se necessário. Depois escolha <b>Enviar para &gt; Área de Trabalho (criar atalho)</b>. "
         "Na Área de Trabalho, você pode renomear o atalho para <b>Trama</b>. Use-o nas próximas aberturas.")
    story.extend([p("O atalho aponta para a pasta original. Não apague nem mova essa pasta depois. "
                    "Se mudar o local, crie o atalho novamente. O atalho não é uma cópia do aplicativo.", note), PageBreak()])
    story.extend([p("Primeiro teste e dúvidas", title),
                  p("Faça uma conferência simples antes de começar a usar suas anotações.", subtitle)])
    step(5, "Crie, salve e reabra uma página",
         "Crie um espaço, um caderno e uma página. Digite um texto. Nas novas versões, aguarde "
         "<b>2 segundos sem digitar</b> e confirme a mensagem <b>Página salva neste computador</b>. "
         "O botão <b>Salvar página</b> continua disponível. Feche e reabra pelo atalho; confira o conteúdo. "
         "Se recebeu um pacote antigo com o indicador <b>Salvamento manual</b>, clique em Salvar antes de sair.")
    step(6, "Experimente os recursos",
         "Selecione um trecho e aplique negrito/itálico. Acrescente um bloco de código e uma imagem PNG/JPEG "
         "de até 10 MiB. Para print da área de transferência, use <b>Colar print</b> ou <b>Ctrl+Shift+V</b>. "
         "Salve e reabra. Teste também uma tag em minúsculas buscando por ela em maiúsculas, e vice-versa.")
    step(7, "Entenda onde ficam suas anotações",
         "A primeira abertura começa com uma biblioteca separada da biblioteca de quem enviou o aplicativo. "
         "Os dados ficam no seu perfil Windows, normalmente em <b>%LOCALAPPDATA%\\Trama</b>, fora da pasta do programa. "
         "Use o recurso de <b>Backup</b> para preservar suas notas; copiar somente o exe não copia seus dados.")
    step(8, "Se houver bloqueio ou erro",
         "Este pacote de teste ainda não tem assinatura digital. Se o Windows bloquear a abertura, "
         "envie a mensagem completa ou um print para quem lhe passou o Trama. "
         "<b>Não desative Smart App Control, antivírus ou outras proteções.</b> "
         "Se aparecer erro de DLL ausente, confira se extraiu todos os arquivos e manteve a pasta completa.")
    story.append(p("Ao relatar um problema, informe a versão/data do ZIP, a versão do Windows, o que tentou fazer "
                   "e a mensagem apresentada. Oculte informações pessoais dos prints. Antes de trocar o pacote "
                   "por uma atualização, faça backup e feche o Trama.", note))
    story.append(p('Referência para criar atalhos: <link href="https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-desktop-icons-in-windows" color="#087D7A">Suporte Microsoft - ícones e atalhos da Área de Trabalho</link>.', small))

    def decorate(canvas, doc):
        width, height = doc.pagesize
        canvas.setFillColor(TEAL)
        canvas.rect(0, height - 10, width, 10, stroke=0, fill=1)
        logo = ROOT / "assets/brand/trama-logo.png"
        if logo.exists():
            canvas.drawImage(ImageReader(str(logo)), 46, height - 62, width=94, height=30,
                             preserveAspectRatio=True, anchor="c", mask="auto")
        canvas.setStrokeColor(colors.HexColor("#D7E2E5"))
        canvas.line(46, 46, width - 46, 46)
        canvas.setFillColor(MUTED)
        canvas.setFont("Guide", 8)
        canvas.drawString(46, 31, "TRAMA  |  Guia do testador  |  29/10/2026")
        canvas.drawRightString(width - 46, 31, f"{doc.page}")

    document = SimpleDocTemplate(str(OUTPUT), pagesize=(595.276, 841.89),
                                 rightMargin=46, leftMargin=46, topMargin=85, bottomMargin=65,
                                 title="Trama - Guia de execução no Windows", author="Projeto Trama")
    document.build(story, onFirstPage=decorate, onLaterPages=decorate)
    print(OUTPUT)


if __name__ == "__main__":
    build()
