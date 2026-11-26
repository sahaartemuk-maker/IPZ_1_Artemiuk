import sys, os, textwrap
from PIL import Image as PI, ImageDraw, ImageFont
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import cm
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY, TA_RIGHT
from reportlab.lib import colors
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Image, PageBreak, KeepTogether, Table, TableStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont

RES, OUT, TMP = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(TMP, exist_ok=True)
F = "/usr/share/fonts/truetype/dejavu/"

# ---------- рендер виводу консолі ----------
def term(lines, path, title, width=118, color_fn=None):
    font = ImageFont.truetype(F + "DejaVuSansMono.ttf", 26)
    wrapped = []
    for ln in lines:
        ln = ln.rstrip("\n")
        parts = textwrap.wrap(ln, width, drop_whitespace=False) or [""]
        wrapped += [(p, color_fn(ln) if color_fn else (220, 220, 220)) for p in parts]
    lh, pad, bar = 34, 24, 50
    W = int(font.getlength("M" * width)) + 2 * pad
    H = bar + pad * 2 + lh * len(wrapped)
    img = PI.new("RGB", (W, H), (30, 30, 30))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W, bar], fill=(58, 58, 58))
    for i, c in enumerate([(255, 95, 86), (255, 189, 46), (39, 201, 63)]):
        d.ellipse([20 + i * 34, 15, 40 + i * 34, 35], fill=c)
    tf = ImageFont.truetype(F + "DejaVuSans.ttf", 24)
    d.text(((W - tf.getlength(title)) / 2, 11), title, font=tf, fill=(210, 210, 210))
    y = bar + pad
    for txt, col in wrapped:
        d.text((pad, y), txt, font=font, fill=col)
        y += lh
    img.save(path)
    return path

def srv_color(l):
    if " << " in l or "<<  " in l: return (90, 200, 230)
    if " >> " in l or ">>  " in l: return (110, 210, 110)
    if " ERR " in l: return (240, 90, 90)
    return (210, 210, 210)

def cli_color(l):
    if l.startswith("==="): return (255, 200, 80)
    if l.startswith(">>"): return (90, 200, 230)
    return (210, 210, 210)

# ---------- PDF ----------
pdfmetrics.registerFont(TTFont("S", F + "DejaVuSerif.ttf"))
pdfmetrics.registerFont(TTFont("SB", F + "DejaVuSerif-Bold.ttf"))
pdfmetrics.registerFont(TTFont("M", F + "DejaVuSansMono.ttf"))
pdfmetrics.registerFontFamily("S", normal="S", bold="SB")
c = ParagraphStyle("c", fontName="S", fontSize=13, leading=18, alignment=TA_CENTER)
cb = ParagraphStyle("cb", parent=c, fontName="SB")
big = ParagraphStyle("big", parent=cb, fontSize=20, leading=28)
r = ParagraphStyle("r", parent=c, alignment=TA_RIGHT)
h = ParagraphStyle("h", fontName="SB", fontSize=13, leading=18, spaceBefore=10, spaceAfter=6, keepWithNext=1)
p = ParagraphStyle("p", fontName="S", fontSize=12, leading=17, alignment=TA_JUSTIFY, firstLineIndent=1.0*cm, spaceAfter=4)
cap = ParagraphStyle("cap", fontName="S", fontSize=11, leading=14, alignment=TA_CENTER, spaceBefore=4, spaceAfter=8)
tc = ParagraphStyle("tc", fontName="S", fontSize=9.5, leading=12)
tm = ParagraphStyle("tm", fontName="M", fontSize=8, leading=10)
code = ParagraphStyle("code", fontName="M", fontSize=8.5, leading=11, leftIndent=0.5*cm, spaceAfter=6)
W, H = A4[0] - 4*cm, A4[1] - 4*cm
n = [0]
def fig(path, title, maxh=H*0.6):
    n[0] += 1
    w, hh = PI.open(path).size
    s = min(W / w, maxh / hh)
    return KeepTogether([Image(path, w*s, hh*s), Paragraph(f"Рис. {n[0]}. {title}", cap)])

def table(rows, widths, mono_col=None):
    data = []
    for i, row in enumerate(rows):
        data.append([Paragraph(x, tm if (i > 0 and j == mono_col) else tc) for j, x in enumerate(row)])
    t = Table(data, colWidths=widths, repeatRows=1)
    t.setStyle(TableStyle([("GRID", (0,0), (-1,-1), 0.5, colors.grey),
                           ("BACKGROUND", (0,0), (-1,0), colors.HexColor("#DDE6F0")),
                           ("VALIGN", (0,0), (-1,-1), "TOP")]))
    return t

import json
SC = os.path.join(RES, "..", "screens")
tests = json.load(open(os.path.join(RES, "tests.json"), encoding="utf-8"))
txt = open(os.path.join(RES, "tests.txt"), encoding="utf-8").read().splitlines()
def tcol(l):
    if "[PASS]" in l: return (110, 210, 110)
    if "[FAIL]" in l: return (240, 90, 90)
    if l.startswith("Підсумок"): return (255, 200, 80)
    return (210, 210, 210)
img_t1 = term(txt[:31], f"{TMP}/t1.png", "StudentRating.Tests — тести T01–T10", color_fn=tcol)
img_t2 = term(txt[31:], f"{TMP}/t2.png", "StudentRating.Tests — тести T11–T20", color_fn=tcol)
def sc(name): return os.path.join(SC, name)

def shot(name, title, maxh=H*0.45):
    return fig(sc(name), title, maxh)

s = [Paragraph("МІНІСТЕРСТВО ОСВІТИ І НАУКИ УКРАЇНИ", c),
     Paragraph("НАЦІОНАЛЬНИЙ УНІВЕРСИТЕТ «ЛЬВІВСЬКА ПОЛІТЕХНІКА»", cb),
     Paragraph("Кафедра ЕОМ", c), Spacer(1, 5*cm),
     Paragraph("ЗВІТ", big),
     Paragraph("до лабораторної роботи №4", c),
     Paragraph("з дисципліни «Інженерія програмного забезпечення»", c), Spacer(1, 0.5*cm),
     Paragraph("на тему: «Формування комплексної системи. Додавання функціоналу в клієнтську частину та заповнення БД. Тестування програмного продукту»", c),
     Paragraph("Варіант №2", c), Spacer(1, 3.5*cm),
     Paragraph("Виконав:<br/>студент групи КІ-301<br/>Артемюк О.", r), Spacer(1, 0.6*cm),
     Paragraph("Прийняв:<br/>Цигилик Л.О.", r), Spacer(1, 3.5*cm),
     Paragraph("Львів – 2026", c), PageBreak()]

s += [Paragraph("Тема", h),
      Paragraph("Формування комплексної системи. Додавання функціоналу в клієнтську частину та заповнення БД необхідною інформацією для демонстрації роботи системи. Тестування програмного продукту.", p),
      Paragraph("Мета", h),
      Paragraph("Завершити розробку системи та перевірити валідність її роботи.", p),
      Paragraph("Постановка задачі", h),
      Paragraph("Додати функціонал у клієнтську та серверну частини, щоб забезпечити повне функціонування системи моніторингу рейтингу студентів та пошуку кращих (варіант №2). Внести в БД усю необхідну інформацію для демонстрації роботи системи. Підготувати список тестових процедур (не менше 10) для тестування функціонування всієї системи та виконати їх.", p),
      Paragraph("Що зроблено", h),
      Paragraph("Графічний клієнт (C#, XAML, Avalonia) підключено до сервера з ЛР3: клас ServerApi встановлює TCP-з'єднання, надсилає JSON-пакети та перетворює відповіді у моделі Student і Grade, з якими працює Data binding форм. Тимчасове джерело даних з ЛР2 повністю замінено на сервер: вхід у систему, довідники груп і предметів, рейтинг групи, пошук кращих, картка студента та запис оцінок тепер виконуються через сервер і зберігаються в БД SQLite.", p),
      Paragraph("Додано новий функціонал: поля адреси та порту сервера у вікні входу; діалог «Додати студента» (команда ADD_STUDENT, доступна лише викладачу); завантаження актуальних даних студента з БД перед показом картки; перерахунок рейтингу після збереження оцінок; обробку втрати зв'язку з сервером і повідомлень про відмову (ACCESS_DENIED, ERROR). Усі звернення до сервера виконуються асинхронно, тому інтерфейс не «зависає».", p),
      Paragraph("БД заповнюється автоматично при першому запуску сервера: 3 користувачі (студент, викладач, адміністратор), 5 предметів, 15 студентів у трьох групах (КІ-201, КІ-301, КІ-302) та 75 оцінок. Цього достатньо для демонстрації рейтингу, пошуку та редагування.", p),
      PageBreak()]

s += [Paragraph("Демонстрація роботи системи", h),
      Paragraph("Нижче наведено покрокові знімки вікон клієнта під час роботи з запущеним сервером. Знімки зроблено самою програмою (функція збереження вікна у PNG) у демонстраційному режимі, який послідовно виконує сценарій роботи викладача.", p),
      Paragraph("<b>Крок 1. Вхід із невірним паролем.</b> Сервер повертає ACCESS_DENIED, клієнт показує повідомлення під полями і очищає пароль.", p),
      shot("01_login_wrong_password.png", "Вікно входу: помилка авторизації", H*0.3),
      Paragraph("<b>Крок 2. Вхід викладача.</b> Вказано адресу сервера 127.0.0.1:5050, логін teacher і пароль. Після успішного входу відкривається головне вікно.", p),
      shot("02_login.png", "Вікно входу з даними викладача", H*0.3),
      Paragraph("<b>Крок 3. Рейтинг групи.</b> Після входу клієнт отримує з сервера список груп і предметів. Обрано групу та натиснуто «Показати» – сервер повертає студентів, відсортованих за середнім балом, з номером місця. У верхній панелі – ім'я та роль користувача, внизу – рядок стану.", p),
      shot("03_rating.png", "Головне вікно: рейтинг групи (дані з сервера)"),
      Paragraph("<b>Крок 4. Картка студента.</b> Подвійний клік по рядку відкриває діалог, дані для якого повторно запитуються з БД командою GET_STUDENT.", p),
      shot("04_student_details.png", "Діалог «Картка студента»", H*0.4),
      Paragraph("<b>Крок 5. Пошук кращих.</b> Задано критерії: предмет «Програмування», мінімальний бал 70, кількість 5. Сервер повертає студентів, відсортованих за оцінкою з обраного предмета.", p),
      shot("05_find_best.png", "Вкладка «Пошук кращих» з результатами"),
      Paragraph("<b>Крок 6. Вкладка «Оцінки».</b> Доступна лише викладачу; показує оцінки обраного студента та кнопки «Редагувати оцінки…» і «Додати студента…».", p),
      shot("06_grades_tab.png", "Вкладка «Оцінки» (роль викладача)"),
      Paragraph("<b>Крок 7. Редагування оцінок.</b> Змінено оцінки з вищої математики (95 → 88) та фізики (88 → 90) і натиснуто «Зберегти» – клієнт надсилає команду SAVE_GRADES, сервер записує оцінки в БД у транзакції.", p),
      shot("07_grade_edit.png", "Діалог редагування оцінок", H*0.4),
      Paragraph("<b>Крок 8. Оновлений рейтинг.</b> Після збереження рейтинг перезавантажено з сервера: середній бал студента змінився з 86,60 на 85,60.", p),
      shot("08_rating_after_save.png", "Рейтинг групи після запису оцінок у БД"),
      Paragraph("<b>Крок 9. Додавання студента.</b> Викладач заповнює прізвище, ім'я, групу та курс; сервер додає запис у таблицю Students і повертає новий Id.", p),
      shot("09_add_student.png", "Діалог «Додати студента»", H*0.4),
      Paragraph("<b>Крок 10. Перевірка.</b> Новий студент з'явився в рейтингу групи КІ-301 (оцінок ще немає, тому середній бал 0,00 і останнє місце).", p),
      shot("10_rating_with_new_student.png", "Рейтинг групи КІ-301 з доданим студентом"),
      Paragraph("<b>Крок 11. Про програму.</b>", p),
      shot("11_about.png", "Вікно «Про програму»", H*0.25),
      PageBreak()]

rows = [["№", "Тестова процедура", "Очікуваний результат", "Отриманий результат", "Статус"]]
for t in tests:
    rows.append([t["Id"], t["Name"], t["Expected"], t["Actual"], "PASS" if t["Pass"] else "FAIL"])
tbl = table(rows, [1.25*cm, 4.2*cm, 3.5*cm, 5.85*cm, 1.7*cm])
tbl.setStyle(TableStyle([("TEXTCOLOR", (4,1), (4,-1), colors.HexColor("#1B7F2A"))]))
passed = sum(1 for t in tests if t["Pass"])

s += [Paragraph("Опис тестових процедур та результати", h),
      Paragraph(f"Для перевірки всієї системи розроблено консольну програму StudentRating.Tests, яка виконує {len(tests)} тестових процедур через той самий TCP-протокол, що й графічний клієнт. Тести запускалися на сервері з чистою БД. Кожна процедура надсилає один або кілька запитів, порівнює відповідь з очікуваним результатом і, де потрібно, повторно читає дані з БД, щоб переконатися, що запис дійсно відбувся (або не відбувся). Перевіряються: авторизація та розмежування прав, коректність рейтингу і пошуку, запис і відкат транзакцій, обробка помилкових даних і пакетів, робота з кількома клієнтами одночасно.", p),
      tbl, Spacer(1, 6),
      Paragraph(f"Результат: пройдено <b>{passed} з {len(tests)}</b> тестів. Вивід програми тестування наведено на рисунках нижче.", p),
      fig(img_t1, "Виконання тестів T01–T10", H*0.75),
      fig(img_t2, "Виконання тестів T11–T20", H*0.75),
      Paragraph("Висновок", h),
      Paragraph(f"Під час виконання лабораторної роботи я завершив розробку клієнт-серверної системи моніторингу рейтингу студентів та пошуку кращих. Графічний клієнт підключено до TCP-сервера, а всі дані тепер зберігаються в базі SQLite, яка автоматично заповнюється демонстраційною інформацією. До клієнта додано налаштування підключення, додавання студентів і обробку помилок зв'язку. Покрокова демонстрація показала, що всі сценарії – вхід, перегляд рейтингу, пошук кращих, редагування оцінок і додавання студента – працюють з реальними даними сервера. Для перевірки якості підготовлено {len(tests)} тестових процедур; усі {passed} пройдено успішно, що підтверджує коректність роботи системи, зокрема захисту від некоректних даних і несанкціонованого доступу.", p)]

def num(cv, d):
    if d.page > 1:
        cv.setFont("S", 10); cv.drawCentredString(A4[0]/2, 1.2*cm, str(d.page))
SimpleDocTemplate(OUT, pagesize=A4, leftMargin=2.5*cm, rightMargin=1.5*cm, topMargin=2*cm, bottomMargin=2*cm,
                  title="ЛР4 ІПЗ – Артемюк О., КІ-301", author="Артемюк О.").build(s, onFirstPage=num, onLaterPages=num)
