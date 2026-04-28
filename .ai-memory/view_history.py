#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
AI Memory System - History Viewer
Просмотр истории взаимодействий с AI
"""

import sqlite3
import sys
from pathlib import Path
from datetime import datetime
from typing import Optional

# Фикс кодировки для Windows
if sys.platform == 'win32':
    import codecs
    sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer, 'strict')
    sys.stderr = codecs.getwriter('utf-8')(sys.stderr.buffer, 'strict')

DB_PATH = Path(__file__).parent / "memory.db"

def print_header(text: str):
    """Красивый заголовок"""
    print("\n" + "="*60)
    print(f"  {text}")
    print("="*60)

def show_recent_sessions(limit: int = 10):
    """Показать последние сессии"""
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    cursor = conn.cursor()
    
    cursor.execute("""
        SELECT 
            s.id,
            s.title,
            s.summary,
            datetime(s.time_created/1000, 'unixepoch', 'localtime') as created,
            datetime(s.time_updated/1000, 'unixepoch', 'localtime') as updated,
            (SELECT COUNT(*) FROM message m WHERE m.session_id = s.id) as msg_count,
            (SELECT GROUP_CONCAT(t.tag, ', ') FROM tag t WHERE t.session_id = s.id) as tags
        FROM session s
        WHERE s.parent_id IS NULL
        ORDER BY s.time_updated DESC
        LIMIT ?
    """, (limit,))
    
    print_header("ПОСЛЕДНИЕ СЕССИИ")
    
    for row in cursor.fetchall():
        print(f"\n📋 ID: {row['id']}")
        print(f"   Название: {row['title']}")
        if row['summary']:
            print(f"   Описание: {row['summary']}")
        print(f"   Создана: {row['created']}")
        print(f"   Обновлена: {row['updated']}")
        print(f"   Сообщений: {row['msg_count']}")
        if row['tags']:
            print(f"   Теги: {row['tags']}")
    
    conn.close()

def show_session_details(session_id: str):
    """Показать детали конкретной сессии"""
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    cursor = conn.cursor()
    
    # Информация о сессии
    cursor.execute("""
        SELECT 
            s.title,
            s.summary,
            datetime(s.time_created/1000, 'unixepoch', 'localtime') as created,
            p.name as project_name
        FROM session s
        LEFT JOIN project p ON p.id = s.project_id
        WHERE s.id = ?
    """, (session_id,))
    
    session = cursor.fetchone()
    if not session:
        print(f"❌ Сессия {session_id} не найдена")
        conn.close()
        return
    
    print_header(f"СЕССИЯ: {session['title']}")
    print(f"Проект: {session['project_name']}")
    print(f"Создана: {session['created']}")
    if session['summary']:
        print(f"Описание: {session['summary']}")
    
    # Сообщения
    cursor.execute("""
        SELECT 
            role,
            content,
            datetime(time_created/1000, 'unixepoch', 'localtime') as created
        FROM message
        WHERE session_id = ?
        ORDER BY time_created ASC
    """, (session_id,))
    
    print("\n" + "-"*60)
    print("СООБЩЕНИЯ:")
    print("-"*60)
    
    for msg in cursor.fetchall():
        role_icon = "👤" if msg['role'] == 'user' else "🤖"
        print(f"\n{role_icon} {msg['role'].upper()} [{msg['created']}]")
        print(f"{msg['content'][:500]}{'...' if len(msg['content']) > 500 else ''}")
    
    # Изменения файлов
    cursor.execute("""
        SELECT 
            file_path,
            action,
            datetime(time_created/1000, 'unixepoch', 'localtime') as created
        FROM file_change
        WHERE session_id = ?
        ORDER BY time_created ASC
    """, (session_id,))
    
    changes = cursor.fetchall()
    if changes:
        print("\n" + "-"*60)
        print("ИЗМЕНЕНИЯ ФАЙЛОВ:")
        print("-"*60)
        for change in changes:
            print(f"📝 {change['action'].upper()}: {change['file_path']} [{change['created']}]")
    
    conn.close()

def search_content(query: str, limit: int = 20):
    """Поиск по содержимому"""
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    cursor = conn.cursor()
    
    cursor.execute("""
        SELECT 
            s.id as session_id,
            s.title,
            m.role,
            m.content,
            datetime(m.time_created/1000, 'unixepoch', 'localtime') as created
        FROM message m
        JOIN session s ON s.id = m.session_id
        WHERE m.content LIKE ?
        ORDER BY m.time_created DESC
        LIMIT ?
    """, (f"%{query}%", limit))
    
    print_header(f"ПОИСК: '{query}'")
    
    results = cursor.fetchall()
    if not results:
        print("\n❌ Ничего не найдено")
    else:
        print(f"\n✓ Найдено результатов: {len(results)}")
        for row in results:
            print(f"\n📋 Сессия: {row['title']} ({row['session_id']})")
            print(f"   Роль: {row['role']}")
            print(f"   Дата: {row['created']}")
            # Показываем контекст вокруг найденного текста
            content = row['content']
            query_lower = query.lower()
            content_lower = content.lower()
            pos = content_lower.find(query_lower)
            if pos != -1:
                start = max(0, pos - 100)
                end = min(len(content), pos + len(query) + 100)
                snippet = content[start:end]
                if start > 0:
                    snippet = "..." + snippet
                if end < len(content):
                    snippet = snippet + "..."
                print(f"   Контекст: {snippet}")
    
    conn.close()

def show_stats():
    """Показать статистику"""
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    print_header("СТАТИСТИКА")
    
    # Общая статистика
    cursor.execute("SELECT COUNT(*) FROM project")
    projects = cursor.fetchone()[0]
    
    cursor.execute("SELECT COUNT(*) FROM session WHERE parent_id IS NULL")
    sessions = cursor.fetchone()[0]
    
    cursor.execute("SELECT COUNT(*) FROM message")
    messages = cursor.fetchone()[0]
    
    cursor.execute("SELECT COUNT(*) FROM file_change")
    file_changes = cursor.fetchone()[0]
    
    print(f"\n📊 Проектов: {projects}")
    print(f"📋 Сессий: {sessions}")
    print(f"💬 Сообщений: {messages}")
    print(f"📝 Изменений файлов: {file_changes}")
    
    # Самые активные теги
    cursor.execute("""
        SELECT tag, COUNT(*) as count
        FROM tag
        GROUP BY tag
        ORDER BY count DESC
        LIMIT 10
    """)
    
    tags = cursor.fetchall()
    if tags:
        print("\n🏷️  Популярные теги:")
        for tag, count in tags:
            print(f"   {tag}: {count}")
    
    # Активность по дням
    cursor.execute("""
        SELECT 
            date(time_created/1000, 'unixepoch', 'localtime') as day,
            COUNT(*) as count
        FROM session
        WHERE parent_id IS NULL
        GROUP BY day
        ORDER BY day DESC
        LIMIT 7
    """)
    
    activity = cursor.fetchall()
    if activity:
        print("\n📅 Активность (последние 7 дней):")
        for day, count in activity:
            print(f"   {day}: {count} сессий")
    
    conn.close()

def show_config():
    """Показать конфигурацию AI"""
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    cursor = conn.cursor()
    
    cursor.execute("""
        SELECT key, value, description
        FROM ai_config
        ORDER BY key
    """)
    
    print_header("КОНФИГУРАЦИЯ AI")
    
    for row in cursor.fetchall():
        print(f"\n🔧 {row['key']}: {row['value']}")
        if row['description']:
            print(f"   {row['description']}")
    
    conn.close()

def main():
    if not DB_PATH.exists():
        print("❌ База данных не найдена. Запустите init_db.py сначала.")
        sys.exit(1)
    
    if len(sys.argv) < 2:
        print("Использование:")
        print("  python view_history.py sessions [limit]     - показать последние сессии")
        print("  python view_history.py session <id>         - показать детали сессии")
        print("  python view_history.py search <query>       - поиск по содержимому")
        print("  python view_history.py stats                - показать статистику")
        print("  python view_history.py config               - показать конфигурацию AI")
        sys.exit(0)
    
    command = sys.argv[1]
    
    if command == "sessions":
        limit = int(sys.argv[2]) if len(sys.argv) > 2 else 10
        show_recent_sessions(limit)
    
    elif command == "session":
        if len(sys.argv) < 3:
            print("❌ Укажите ID сессии")
            sys.exit(1)
        show_session_details(sys.argv[2])
    
    elif command == "search":
        if len(sys.argv) < 3:
            print("❌ Укажите поисковый запрос")
            sys.exit(1)
        query = " ".join(sys.argv[2:])
        search_content(query)
    
    elif command == "stats":
        show_stats()
    
    elif command == "config":
        show_config()
    
    else:
        print(f"❌ Неизвестная команда: {command}")
        sys.exit(1)

if __name__ == "__main__":
    main()
