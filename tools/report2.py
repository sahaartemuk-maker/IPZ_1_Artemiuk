import sys, os, glob
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import cm
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY, TA_RIGHT
from reportlab.lib import colors
from reportlab.platypus import (SimpleDocTemplate, Paragraph, Spacer, Image, PageBreak, KeepTogether, Table, TableStyle)
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from PIL import Image as PI

F = "/usr/share/fonts/truetype/dejavu/"
pdfmetrics.registerFont(TTFont("S", F + "DejaVuSerif.ttf"))
pdfmetrics.registerFont(TTFont("SB", F + "DejaVuSerif-Bold.ttf"))
pdfmetrics.registerFont(TTFont("M", F + "DejaVuSansMono.ttf"))
pdfmetrics.registerFontFamily("S", normal="S", bold="SB")

OUT, SHOTS = sys.argv[1], sys.argv[2]   # SHOTS: папка зі скріншотами 1.png..6.png

c = ParagraphStyle("c", fontName="S", fontSize=13, leading=18, alignment=TA_CENTER)
cb = ParagraphStyle("cb", parent=c, fontName="SB")
big = ParagraphStyle("big", parent=cb, fontSize=20, leading=28)
r = ParagraphStyle("r", parent=c, alignment=TA_RIGHT)
h = ParagraphStyle("h", fontName="SB", fontSize=13, leading=18, spaceBefore=10, spaceAfter=6, keepWithNext=1)
p = ParagraphStyle("p", fontName="S", fontSize=12, leading=17, alignment=TA_JUSTIFY, firstLineIndent=1.0*cm, spaceAfter=4)
li = ParagraphStyle("li", parent=p, firstLineIndent=0, leftIndent=0.6*cm, bulletIndent=0)
cap = ParagraphStyle("cap", fontName="S", fontSize=11, leading=14, alignment=TA_CENTER, spaceBefore=4, spaceAfter=8)
tc = ParagraphStyle("tc", fontName="S", fontSize=10, leading=13)
code = ParagraphStyle("code", fontName="M", fontSize=8.5, leading=11, leftIndent=0.5*cm, spaceAfter=6)

W, H = A4[0] - 4*cm, A4[1] - 4*cm
n = [0]
def shot(i, title):
    n[0] += 1
    path = os.path.join(SHOTS, f"{i}.png")
    if os.path.exists(path):
        w, hh = PI.open(path).size
        s = min(W / w, H*0.55 / hh)
        el = Image(path, w*s, hh*s)
    else:
        el = Table([[Paragraph(f"[ Скріншот {i}: {title} ]", c)]], colWidths=[W], rowHeights=[6*cm])
        el.setStyle(TableStyle([("BOX", (0,0), (-1,-1), 1, colors.grey), ("VALIGN", (0,0), (-1,-1), "MIDDLE")]))
    return KeepTogether([el, Paragraph(f"Рис. {n[0]}. {title}", cap)])

def table(rows, widths):
    t = Table([[Paragraph(x, tc) for x in row] for row in rows], colWidths=widths, repeatRows=1)
    t.setStyle(TableStyle([("GRID", (0,0), (-1,-1), 0.5, colors.grey),
                           ("BACKGROUND", (0,0), (-1,0), colors.HexColor("#DDE6F0")),
                           ("VALIGN", (0,0), (-1,-1), "TOP")]))
    return t

s = [Paragraph("МІНІСТЕРСТВО ОСВІТИ І НАУКИ УКРАЇНИ", c),
     Paragraph("НАЦІОНАЛЬНИЙ УНІВЕРСИТЕТ «ЛЬВІВСЬКА ПОЛІТЕХНІКА»", cb),
     Paragraph("Кафедра ЕОМ", c), Spacer(1, 5*cm),
     Paragraph("ЗВІТ", big),
     Paragraph("до лабораторної роботи №2", c),
     Paragraph("з дисципліни «Інженерія програмного забезпечення»", c), Spacer(1, 0.5*cm),
     Paragraph("на тему: «Створення Windows Forms на С# за технологією XAML. Розробка оболонки GUI для реалізації системи згідно варіанту»", c),
     Paragraph("Варіант №2", c), Spacer(1, 3.5*cm),
     Paragraph("Виконав:<br/>студент групи КІ-301<br/>Артемюк О.", r), Spacer(1, 0.6*cm),
     Paragraph("Прийняв:<br/>Цигилик Л.О.", r), Spacer(1, 3.5*cm),
     Paragraph("Львів – 2026", c), PageBreak()]

s += [Paragraph("Тема", h),
      Paragraph("Створення Windows Forms на С# за технологією XAML. Розробка оболонки GUI для реалізації системи згідно варіанту.", p),
      Paragraph("Мета", h),
      Paragraph("Освоїти основи створення WinForms за технологією XAML. Розробити GUI-оболонку для реалізації клієнтської частини системи згідно з варіантом.", p),
      Paragraph("Постановка задачі", h),
      Paragraph("Розробити графічний інтерфейс клієнтської частини системи моніторингу рейтингу студентів та пошуку кращих (варіант №2) мовою C# з використанням технології XAML (WPF). У програмі необхідно використати:", p),
      Paragraph("• Data binding – елементи форми прив'язані до конкретних полів класу;", li),
      Paragraph("• багатовіконний інтерфейс (діалогові вікна);", li),
      Paragraph("• збереження стилів і кольорів кнопок та інших елементів форми в окремому файлі;", li),
      Paragraph("• методи GET та SET для полів класів;", li),
      Paragraph("• конструкцію try{}catch{} для перехоплення можливих помилок у роботі програми.", li),
      Paragraph("Реалізація вимог", h),
      Paragraph("Проєкт <b>StudentRating.Client</b> створено як WPF-застосунок для .NET Framework. Вихідний код розміщено в репозиторії в папці «Лабораторна_№2/Проект». Структура проєкту:", p),
      Paragraph("Models/ – класи Student, Grade, User, SearchCriteria, ObservableObject<br/>"
                "Views/ – вікна LoginWindow, MainWindow, StudentDetailsWindow, GradeEditDialog, AboutWindow<br/>"
                "Styles/Styles.xaml – словник ресурсів зі стилями та кольорами<br/>"
                "Services/DataService.cs – тимчасове джерело даних (до появи сервера в ЛР3)", code),
      Paragraph("<b>Data binding.</b> Усі моделі наслідують клас ObservableObject, який реалізує інтерфейс INotifyPropertyChanged, тому зміна властивості одразу відображається на формі. Наприклад, поле «Логін» прив'язане до User.Login, поля форми пошуку – до властивостей класу SearchCriteria, таблиці – до колекцій ObservableCollection&lt;Student&gt;, а видимість вкладки «Оцінки» – до властивості User.IsTeacher через BooleanToVisibilityConverter.", p),
      Paragraph("<b>Багатовіконний інтерфейс.</b> Програма складається з п'яти вікон: вікна входу, головного вікна та трьох модальних діалогових вікон (картка студента, редагування оцінок, «Про програму»), які відкриваються методом ShowDialog().", p),
      Paragraph("<b>Стилі в окремому файлі.</b> Кольори (SolidColorBrush) та стилі кнопок, полів вводу, таблиць і заголовків описані у файлі Styles/Styles.xaml, який підключається до App.xaml через MergedDictionaries. Вікна посилаються на них через {StaticResource PrimaryButton}, {StaticResource HeaderText} тощо.", p),
      Paragraph("<b>GET і SET.</b> Кожне поле класу є приватним і доступне лише через властивість з методами get та set. Сеттери Grade.Score, SearchCriteria.MinScore і SearchCriteria.TopCount перевіряють діапазон значень і генерують виняток ArgumentOutOfRangeException, який форма показує як помилку валідації (ValidatesOnExceptions=True).", p),
      Paragraph("<b>try{}catch{}.</b> Вхід у систему, завантаження рейтингу, пошук, збереження оцінок та експорт у CSV виконуються в блоках try/catch з окремою обробкою типових винятків: UnauthorizedAccessException, ArgumentException, IOException та InvalidOperationException. Додатково в App.xaml.cs встановлено глобальний обробник DispatcherUnhandledException.", p),
      Paragraph("<b>Кросплатформна версія.</b> Оскільки WPF працює лише у Windows, для запуску на macOS клієнт також реалізовано на Avalonia UI 11 (.NET 8) – це той самий C# і XAML-подібна розмітка (файли .axaml). Проєкт StudentRating.Avalonia використовує ті самі класи моделей і сервіс даних, а стилі винесені в окремий файл Styles/AppStyles.axaml.", p),
      PageBreak()]

s += [Paragraph("Опис форм", h),
      Paragraph("У таблиці наведено вікна програми, використані елементи інтерфейсу та їх призначення.", p),
      table([["Вікно", "Елементи UI", "Призначення"],
             ["LoginWindow – вхід", "TextBlock, TextBox, PasswordBox, Button «Увійти», «Вихід»", "Авторизація користувача. Помилки (порожні поля, невірний пароль) виводяться червоним текстом під полями."],
             ["MainWindow – головне", "Верхня панель (Border, TextBlock з Run), TabControl з трьома TabItem, рядок стану", "Показує ім'я та роль користувача, кнопки «Про програму» і «Вийти», робочі вкладки."],
             ["Вкладка «Рейтинг групи»", "ComboBox групи, Button «Показати», «Детально…», «Експорт у CSV», DataGrid", "Перегляд рейтингу групи (місце, ПІБ, група, курс, середній бал). Експорт доступний лише викладачу."],
             ["Вкладка «Пошук кращих»", "ComboBox курсу, групи та предмета, TextBox мінімального балу та N, Button «Знайти», «Скинути», DataGrid", "Пошук топ-N студентів за критеріями. Некоректні значення підсвічуються червоною рамкою."],
             ["Вкладка «Оцінки»", "Button «Редагувати оцінки…», Border з ItemsControl", "Доступна лише викладачу. Показує оцінки обраного студента."],
             ["StudentDetailsWindow", "TextBlock, Grid, DataGrid, Button «Закрити»", "Модальний діалог з карткою студента та списком його оцінок."],
             ["GradeEditDialog", "Редагована DataGrid, Button «Зберегти», «Скасувати»", "Модальний діалог редагування оцінок з перевіркою діапазону 0..100. Працює з копією даних."],
             ["AboutWindow", "TextBlock, Button «OK»", "Інформація про програму та автора."]],
            [3.6*cm, 5.6*cm, 7.3*cm]),
      PageBreak()]

s += [Paragraph("Скріншоти форм", h)]
for i, t in enumerate(["Вікно входу в систему", "Головне вікно – рейтинг групи",
                       "Вкладка «Пошук кращих» з результатами", "Діалогове вікно «Картка студента»",
                       "Діалог редагування оцінок (помилка валідації)", "Вкладка «Оцінки» (роль викладача)"], 1):
    s.append(shot(i, t))

s += [Paragraph("Висновок", h),
      Paragraph("Під час виконання лабораторної роботи я освоїв основи створення графічного інтерфейсу мовою C# за технологією XAML (WPF) і розробив GUI-оболонку клієнтської частини системи моніторингу рейтингу студентів та пошуку кращих. У програмі реалізовано прив'язку даних (Data binding) елементів форми до властивостей класів через INotifyPropertyChanged, а також багатовіконний інтерфейс із трьома модальними діалогами. Стилі та кольори елементів винесено в окремий файл ресурсів Styles.xaml. Поля класів інкапсульовано у властивості з методами get і set, частина з яких перевіряє коректність значень. Обробку можливих помилок виконано за допомогою конструкції try{}catch{}. Наразі дані надає тимчасовий клас DataService; у наступній роботі його буде замінено на обмін із сервером за протоколом TCP.", p)]

def num(cv, d):
    if d.page > 1:
        cv.setFont("S", 10); cv.drawCentredString(A4[0]/2, 1.2*cm, str(d.page))
SimpleDocTemplate(OUT, pagesize=A4, leftMargin=2.5*cm, rightMargin=1.5*cm, topMargin=2*cm, bottomMargin=2*cm,
                  title="ЛР2 ІПЗ – Артемюк О., КІ-301", author="Артемюк О.").build(s, onFirstPage=num, onLaterPages=num)
