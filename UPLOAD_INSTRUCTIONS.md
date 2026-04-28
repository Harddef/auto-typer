# Инструкция по загрузке на GitHub

## Вариант 1: Через веб-интерфейс (самый простой)

1. Зайдите на https://github.com и войдите в свой аккаунт
2. Нажмите зеленую кнопку "New" (или "+" в правом верхнем углу → "New repository")
3. Заполните:
   - Repository name: `auto-typer`
   - Description: `Modern Windows application for automated text input`
   - Public/Private: выберите что хотите
   - НЕ ставьте галочки на "Add README" и "Add .gitignore"
4. Нажмите "Create repository"
5. На следующей странице нажмите "uploading an existing file"
6. Перетащите все файлы из папки `D:\отзыв` в окно браузера:
   - MainForm.cs
   - WindowSelectorForm.cs
   - KeyboardSimulator.cs
   - NativeMethods.cs
   - Program.cs
   - AutoTyper.csproj
   - app.manifest
   - README.md
   - LICENSE
   - .gitignore
7. Напишите commit message: "Initial commit"
8. Нажмите "Commit changes"

Готово! Ваш проект на GitHub.

## Вариант 2: Через Git командную строку

Откройте PowerShell в папке `D:\отзыв` и выполните:

```bash
# Инициализация репозитория
git init

# Добавить все файлы
git add .

# Создать первый коммит
git commit -m "Initial commit: Auto Typer application"

# Подключить удаленный репозиторий (замените YOUR_USERNAME на ваш GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/auto-typer.git

# Отправить на GitHub
git branch -M main
git push -u origin main
```

## Что загружать

Загружайте только исходный код:
- ✅ Все .cs файлы
- ✅ AutoTyper.csproj
- ✅ app.manifest
- ✅ README.md
- ✅ LICENSE
- ✅ .gitignore
- ❌ НЕ загружайте папки bin/, obj/, publish/
- ❌ НЕ загружайте .exe файлы (их можно добавить в Releases)

## Создание Release с .exe файлом

После загрузки кода:
1. Перейдите в ваш репозиторий на GitHub
2. Нажмите "Releases" → "Create a new release"
3. Tag version: `v1.0.0`
4. Release title: `Auto Typer v1.0.0`
5. Description: опишите что в релизе
6. Прикрепите файл `publish\AutoTyper.exe`
7. Нажмите "Publish release"

Теперь пользователи смогут скачать готовый .exe файл!
