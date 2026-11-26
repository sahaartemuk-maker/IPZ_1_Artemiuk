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

srv = open(os.path.join(RES, "server.log"), encoding="utf-8").read().splitlines()
cli = open(os.path.join(RES, "client.log"), encoding="utf-8").read().splitlines()
db = open(os.path.join(RES, "db.txt"), encoding="utf-8").read().split("\n\n")

def short(l, n=230):
    return l if len(l) <= n else l[:n] + "…"

img_srv = term([short(l) for l in srv], f"{TMP}/srv.png", "StudentRating.Server — консоль сервера", color_fn=srv_color)

def cli_block(start, end):
    out, on = [], False
    for l in cli:
        if l.startswith("=== " + start): on = True
        elif on and l.startswith("=== " + end): break
        if on: out.append(l)
    return [l for l in out if l.strip() != ""] if out else ["(немає даних)"]

img_cli1 = term(cli_block("4.", "5.") , f"{TMP}/cli1.png", "StudentRating.TestClient — авторизація", color_fn=cli_color)
img_cli2 = term(cli_block("8.", "11."), f"{TMP}/cli2.png", "StudentRating.TestClient — запис оцінок у БД", color_fn=cli_color)
img_cli3 = term(cli_block("1.", "4."), f"{TMP}/cli3.png", "StudentRating.TestClient — перевірка помилок", color_fn=cli_color)
img_db1 = term(db[0].splitlines(), f"{TMP}/db1.png", "sqlite3 rating.db — таблиця Grades", width=100)
img_db2 = term(db[1].splitlines(), f"{TMP}/db2.png", "sqlite3 rating.db — таблиця RequestLog", width=100)

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

s = [Paragraph("МІНІСТЕРСТВО ОСВІТИ І НАУКИ УКРАЇНИ", c),
     Paragraph("НАЦІОНАЛЬНИЙ УНІВЕРСИТЕТ «ЛЬВІВСЬКА ПОЛІТЕХНІКА»", cb),
     Paragraph("Кафедра ЕОМ", c), Spacer(1, 5*cm),
     Paragraph("ЗВІТ", big),
     Paragraph("до лабораторної роботи №3", c),
     Paragraph("з дисципліни «Інженерія програмного забезпечення»", c), Spacer(1, 0.5*cm),
     Paragraph("на тему: «Розробка серверної частини. Розробка комунікації за протоколом TCP. Підключення серверного модуля до БД»", c),
     Paragraph("Варіант №2", c), Spacer(1, 3.5*cm),
     Paragraph("Виконав:<br/>студент групи КІ-301<br/>Артемюк О.", r), Spacer(1, 0.6*cm),
     Paragraph("Прийняв:<br/>Цигилик Л.О.", r), Spacer(1, 3.5*cm),
     Paragraph("Львів – 2026", c), PageBreak()]

s += [Paragraph("Тема", h),
      Paragraph("Розробка серверної частини. Розробка комунікації за протоколом TCP. Підключення серверного модуля до БД.", p),
      Paragraph("Мета", h),
      Paragraph("Розробити консольну аплікацію, що підтримує зв'язок за протоколом TCP/IP, отримує дані та записує їх у БД, а також за відповідною командою вичитує з БД необхідну інформацію та передає її за протоколом TCP на клієнтську частину.", p),
      Paragraph("Постановка задачі", h),
      Paragraph("Для системи моніторингу рейтингу студентів та пошуку кращих (варіант №2) розробити серверну частину у вигляді консольного застосунку на C#. Сервер повинен приймати підключення клієнтів за протоколом TCP, обробляти команди (авторизація, отримання рейтингу групи, пошук кращих студентів, запис оцінок), зберігати дані в реляційній базі даних і повертати результати клієнту. Для перевірки роботи сервера розробити тестовий консольний клієнт.", p),
      Paragraph("Архітектура серверної частини", h),
      Paragraph("Рішення складається з трьох частин: спільного опису протоколу (Protocol.cs), сервера StudentRating.Server та тестового клієнта StudentRating.TestClient. Сервер реалізовано на .NET 8 з використанням класу TcpListener: він слухає порт 5050 і для кожного підключеного клієнта створює окремий потік, тому одночасно можуть працювати кілька клієнтів. Як СУБД використано SQLite (бібліотека Microsoft.Data.Sqlite) – база зберігається у файлі rating.db і створюється та заповнюється демонстраційними даними автоматично при першому запуску. Усі SQL-запити параметризовані, що захищає від SQL-ін'єкцій, а паролі зберігаються у вигляді хешу SHA-256.", p),
      Paragraph("Сервер складається з класів: <b>TcpServer</b> – приймання з'єднань і цикл читання пакетів; <b>CommandHandler</b> – розбір команд, перевірка авторизації та прав доступу, формування відповіді; <b>Database</b> – створення схеми БД і всі операції читання/запису; <b>Logger</b> – журнал подій у консоль та файл server.log. Кожен оброблений запит додатково записується в таблицю RequestLog.", p),
      Paragraph("Формат пакетів", h),
      Paragraph("Обмін відбувається текстовими пакетами у форматі JSON, кожен пакет – один рядок UTF-8, що завершується символом переводу рядка. Клієнт надсилає запит, сервер повертає відповідь у тому ж з'єднанні:", p),
      Paragraph('Запит:&nbsp;&nbsp;&nbsp;{"Command":"GET_RATING","Token":"7147…","Args":{"group":"КІ-301"}}<br/>'
                'Відповідь: {"Status":"OK","Message":"Студентів у групі КІ-301: 5","Data":[…]}', code),
      Paragraph("Поле Status може мати значення OK, ERROR (помилка даних або параметрів), ACCESS_DENIED (немає авторизації чи прав) та UNKNOWN_COMMAND. Після успішної команди LOGIN сервер видає клієнту токен сесії, який передається в усіх наступних запитах.", p),
      Paragraph("Структура бази даних", h),
      table([["Таблиця", "Поля", "Призначення"],
             ["Users", "Id, Login, PasswordHash, FullName, Role", "Користувачі системи та їх ролі (Student / Teacher / Admin)"],
             ["Students", "Id, LastName, FirstName, GroupName, Course", "Студенти"],
             ["Subjects", "Id, Name", "Навчальні предмети"],
             ["Grades", "StudentId, SubjectId, Score, UpdatedAt", "Оцінки (0–100), первинний ключ – пара студент/предмет"],
             ["RequestLog", "Id, Time, Client, Command, Status", "Журнал усіх запитів до сервера"]],
            [2.6*cm, 6.2*cm, 7.7*cm]),
      PageBreak()]

s += [Paragraph("Опис API функцій серверної частини", h),
      table([["Команда", "Параметри (Args)", "Доступ", "Результат"],
             ["PING", "–", "усі", "«PONG» – перевірка зв'язку"],
             ["LOGIN", "login, password", "усі", "Дані користувача (ПІБ, роль) і токен сесії; ACCESS_DENIED при невірному паролі"],
             ["GET_GROUPS", "–", "авторизовані", "Список груп і предметів з БД"],
             ["GET_RATING", "group", "авторизовані", "Студенти групи з оцінками, відсортовані за середнім балом, з номером місця"],
             ["FIND_BEST", "course, group, subject, minScore, top", "авторизовані", "Топ-N студентів за середнім балом або за оцінкою з предмета, не нижче мінімального балу"],
             ["GET_STUDENT", "id", "авторизовані", "Картка студента з усіма оцінками"],
             ["SAVE_GRADES", "studentId, grades[{Subject, Score}]", "викладач", "Запис/оновлення оцінок у таблиці Grades в одній транзакції; при помилці – відкат"],
             ["ADD_STUDENT", "lastName, firstName, group, course", "викладач", "Додавання студента, повертає новий Id"]],
            [2.7*cm, 4.4*cm, 2.4*cm, 7.0*cm]),
      Spacer(1, 6),
      Paragraph("Методи класу Database, через які сервер працює з БД: Initialize() – створення таблиць і початкове заповнення; CheckLogin() – перевірка логіна та хешу пароля; GetGroups(), GetSubjects() – довідники; GetStudents(group, course, id) – вибірка студентів з оцінками одним JOIN-запитом і розрахунок середнього балу; SaveGrades() – запис оцінок з перевіркою діапазону та існування студента і предмета (INSERT … ON CONFLICT DO UPDATE у транзакції); AddStudent(); LogRequest() – запис у журнал запитів.", p),
      Paragraph("Обробка помилок виконана конструкціями try/catch на кількох рівнях: некоректний JSON повертає клієнту ERROR, помилки параметрів (ArgumentException, FormatException) перетворюються на зрозумілі повідомлення, розрив з'єднання (IOException) коректно завершує потік клієнта, а помилки запису в БД призводять до відкату транзакції.", p),
      PageBreak()]

s += [Paragraph("Результати роботи", h),
      Paragraph("Для перевірки сервер і тестовий клієнт було запущено на macOS (.NET 11 SDK). Тестовий клієнт надіслав 11 запитів: перевірку зв'язку, запит без авторизації, вхід з невірним і правильним паролем, отримання довідників, рейтингу групи КІ-301, пошук кращих, запис оцінок у БД, перевірку записаних даних, спробу записати некоректну оцінку та невідому команду. Нижче наведено вивід консолі сервера з усіма отриманими (&lt;&lt;) та відправленими (&gt;&gt;) пакетами.", p),
      fig(img_srv, "Консоль сервера: прийом пакетів, обробка та відповіді", H*0.8),
      fig(img_cli1, "Тестовий клієнт: авторизація викладача та отримання токена"),
      fig(img_cli2, "Тестовий клієнт: передача пакета з оцінками, запис у БД і перевірка; відхилення оцінки 150"),
      fig(img_cli3, "Тестовий клієнт: запит без авторизації та невірний пароль"),
      Paragraph("Після роботи клієнта вміст таблиць БД переглянуто утилітою sqlite3. У таблиці Grades для студента з Id=1 оцінки з «Програмування» (98) та «ІПЗ» (95) мають новий час оновлення – вони записані командою SAVE_GRADES. Оцінка 150 з «Фізики» не потрапила в БД, оскільки транзакцію було відкочено. Таблиця RequestLog містить усі 11 запитів зі статусами їх обробки.", p),
      fig(img_db1, "Таблиця Grades після запису оцінок (студент Id=1)"),
      fig(img_db2, "Таблиця RequestLog – журнал запитів до сервера"),
      Paragraph("Висновок", h),
      Paragraph("Під час виконання лабораторної роботи я розробив серверну частину системи моніторингу рейтингу студентів у вигляді консольного застосунку на C#. Сервер приймає з'єднання за протоколом TCP і обслуговує кожного клієнта в окремому потоці. Для обміну даними використано простий текстовий протокол із JSON-пакетами. Дані зберігаються в базі SQLite з п'ятьма таблицями; запис оцінок виконується в транзакції, а всі запити – параметризовані. Реалізовано 8 команд API, зокрема авторизацію з токеном сесії та розмежування прав студента і викладача. Тестування за допомогою консольного клієнта показало, що сервер коректно приймає пакети, записує дані в БД, вичитує їх за командою та передає клієнту, а також правильно обробляє помилкові запити. У наступній роботі сервер буде підключено до графічного клієнта.", p)]

def num(cv, d):
    if d.page > 1:
        cv.setFont("S", 10); cv.drawCentredString(A4[0]/2, 1.2*cm, str(d.page))
SimpleDocTemplate(OUT, pagesize=A4, leftMargin=2.5*cm, rightMargin=1.5*cm, topMargin=2*cm, bottomMargin=2*cm,
                  title="ЛР3 ІПЗ – Артемюк О., КІ-301", author="Артемюк О.").build(s, onFirstPage=num, onLaterPages=num)
