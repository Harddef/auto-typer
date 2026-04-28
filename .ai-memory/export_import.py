#!/usr/bin/env python3
"""
AI Memory System - Context Export/Import
Экспорт и импорт контекста для переноса между AI системами
"""

import sqlite3
import json
import sys
from pathlib import Path
from datetime import datetime
from typing import Dict, List

DB_PATH = Path(__file__).parent / "memory.db"

class ContextExporter:
    """Экспорт контекста в различные форматы"""
    
    def __init__(self):
        self.conn = sqlite3.connect(DB_PATH)
        self.conn.row_factory = sqlite3.Row
    
    def export_session_to_json(self, session_id: str, output_file: str):
        """Экспортировать сессию в JSON"""
        cursor = self.conn.cursor()
        
        # Получаем информацию о сессии
        cursor.execute("""
            SELECT s.*, p.name as project_name, p.path as project_path
            FROM session s
            LEFT JOIN project p ON p.id = s.project_id
            WHERE s.id = ?
        """, (session_id,))
        
        session = cursor.fetchone()
        if not session:
            print(f"❌ Сессия {session_id} не найдена")
            return
        
        # Получаем сообщения
        cursor.execute("""
            SELECT role, content, metadata, time_created
            FROM message
            WHERE session_id = ?
            ORDER BY time_created ASC
        """, (session_id,))
        
        messages = []
        for msg in cursor.fetchall():
            messages.append({
                'role': msg['role'],
                'content': msg['content'],
                'metadata': json.loads(msg['metadata']) if msg['metadata'] else None,
                'timestamp': msg['time_created']
            })
        
        # Получаем изменения файлов
        cursor.execute("""
            SELECT file_path, action, content_before, content_after, time_created
            FROM file_change
            WHERE session_id = ?
            ORDER BY time_created ASC
        """, (session_id,))
        
        file_changes = []
        for change in cursor.fetchall():
            file_changes.append({
                'file_path': change['file_path'],
                'action': change['action'],
                'content_before': change['content_before'],
                'content_after': change['content_after'],
                'timestamp': change['time_created']
            })
        
        # Получаем теги
        cursor.execute("""
            SELECT tag FROM tag WHERE session_id = ?
        """, (session_id,))
        
        tags = [row['tag'] for row in cursor.fetchall()]
        
        # Формируем итоговый JSON
        export_data = {
            'export_version': '1.0',
            'export_date': datetime.now().isoformat(),
            'session': {
                'id': session['id'],
                'title': session['title'],
                'summary': session['summary'],
                'project_name': session['project_name'],
                'project_path': session['project_path'],
                'created': session['time_created'],
                'updated': session['time_updated']
            },
            'messages': messages,
            'file_changes': file_changes,
            'tags': tags
        }
        
        # Сохраняем в файл
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(export_data, f, ensure_ascii=False, indent=2)
        
        print(f"✓ Сессия экспортирована в {output_file}")
        print(f"  Сообщений: {len(messages)}")
        print(f"  Изменений файлов: {len(file_changes)}")
        print(f"  Тегов: {len(tags)}")
    
    def export_all_sessions_to_json(self, output_file: str):
        """Экспортировать все сессии в один JSON файл"""
        cursor = self.conn.cursor()
        
        cursor.execute("""
            SELECT id FROM session WHERE parent_id IS NULL
            ORDER BY time_created DESC
        """)
        
        session_ids = [row['id'] for row in cursor.fetchall()]
        
        all_sessions = []
        for session_id in session_ids:
            # Используем тот же код что и для одной сессии
            cursor.execute("""
                SELECT s.*, p.name as project_name, p.path as project_path
                FROM session s
                LEFT JOIN project p ON p.id = s.project_id
                WHERE s.id = ?
            """, (session_id,))
            
            session = cursor.fetchone()
            
            cursor.execute("""
                SELECT role, content, metadata, time_created
                FROM message WHERE session_id = ?
                ORDER BY time_created ASC
            """, (session_id,))
            
            messages = [dict(msg) for msg in cursor.fetchall()]
            
            all_sessions.append({
                'session': dict(session),
                'messages': messages
            })
        
        export_data = {
            'export_version': '1.0',
            'export_date': datetime.now().isoformat(),
            'total_sessions': len(all_sessions),
            'sessions': all_sessions
        }
        
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(export_data, f, ensure_ascii=False, indent=2)
        
        print(f"✓ Экспортировано {len(all_sessions)} сессий в {output_file}")
    
    def export_to_markdown(self, session_id: str, output_file: str):
        """Экспортировать сессию в Markdown для удобного чтения"""
        cursor = self.conn.cursor()
        
        cursor.execute("""
            SELECT s.*, p.name as project_name
            FROM session s
            LEFT JOIN project p ON p.id = s.project_id
            WHERE s.id = ?
        """, (session_id,))
        
        session = cursor.fetchone()
        if not session:
            print(f"❌ Сессия {session_id} не найдена")
            return
        
        cursor.execute("""
            SELECT role, content, datetime(time_created/1000, 'unixepoch', 'localtime') as created
            FROM message
            WHERE session_id = ?
            ORDER BY time_created ASC
        """, (session_id,))
        
        messages = cursor.fetchall()
        
        # Формируем Markdown
        md_content = f"# {session['title']}\n\n"
        md_content += f"**Проект:** {session['project_name']}\n\n"
        if session['summary']:
            md_content += f"**Описание:** {session['summary']}\n\n"
        md_content += f"**Создана:** {datetime.fromtimestamp(session['time_created']/1000).strftime('%Y-%m-%d %H:%M:%S')}\n\n"
        md_content += "---\n\n"
        
        for msg in messages:
            role_name = "Пользователь" if msg['role'] == 'user' else "Ассистент"
            md_content += f"## {role_name} [{msg['created']}]\n\n"
            md_content += f"{msg['content']}\n\n"
            md_content += "---\n\n"
        
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(md_content)
        
        print(f"✓ Сессия экспортирована в Markdown: {output_file}")
    
    def export_for_ai_prompt(self, session_id: str, output_file: str):
        """Экспортировать сессию в формате для загрузки в AI как контекст"""
        cursor = self.conn.cursor()
        
        cursor.execute("""
            SELECT role, content
            FROM message
            WHERE session_id = ?
            ORDER BY time_created ASC
        """, (session_id,))
        
        messages = cursor.fetchall()
        
        # Формируем промпт для AI
        prompt = "# Контекст предыдущей сессии\n\n"
        prompt += "Ниже представлена история нашего предыдущего взаимодействия. "
        prompt += "Продолжи работу с учетом этого контекста.\n\n"
        prompt += "---\n\n"
        
        for msg in messages:
            role_marker = "USER:" if msg['role'] == 'user' else "ASSISTANT:"
            prompt += f"{role_marker}\n{msg['content']}\n\n"
        
        prompt += "---\n\n"
        prompt += "Продолжи работу с того места, где мы остановились.\n"
        
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(prompt)
        
        print(f"✓ Контекст для AI экспортирован: {output_file}")
        print(f"  Можешь скопировать содержимое файла и вставить в новую сессию AI")
    
    def close(self):
        if self.conn:
            self.conn.close()


class ContextImporter:
    """Импорт контекста из JSON"""
    
    def __init__(self):
        self.conn = sqlite3.connect(DB_PATH)
    
    def import_from_json(self, input_file: str):
        """Импортировать сессию из JSON"""
        with open(input_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        cursor = self.conn.cursor()
        
        # Проверяем версию
        if data.get('export_version') != '1.0':
            print("⚠️  Неизвестная версия экспорта, могут быть проблемы")
        
        session = data['session']
        messages = data['messages']
        file_changes = data.get('file_changes', [])
        tags = data.get('tags', [])
        
        # Импортируем сессию
        cursor.execute("""
            INSERT OR REPLACE INTO session (id, project_id, title, summary, time_created, time_updated)
            VALUES (?, ?, ?, ?, ?, ?)
        """, (
            session['id'],
            'proj_stilak_main',  # Используем дефолтный проект
            session['title'],
            session['summary'],
            session['created'],
            session['updated']
        ))
        
        # Импортируем сообщения
        for msg in messages:
            msg_id = f"msg_{msg['timestamp']}"
            cursor.execute("""
                INSERT OR REPLACE INTO message (id, session_id, role, content, metadata, time_created)
                VALUES (?, ?, ?, ?, ?, ?)
            """, (
                msg_id,
                session['id'],
                msg['role'],
                msg['content'],
                json.dumps(msg['metadata']) if msg['metadata'] else None,
                msg['timestamp']
            ))
        
        # Импортируем изменения файлов
        for change in file_changes:
            cursor.execute("""
                INSERT INTO file_change (session_id, file_path, action, content_before, content_after, time_created)
                VALUES (?, ?, ?, ?, ?, ?)
            """, (
                session['id'],
                change['file_path'],
                change['action'],
                change['content_before'],
                change['content_after'],
                change['timestamp']
            ))
        
        # Импортируем теги
        for tag in tags:
            cursor.execute("""
                INSERT INTO tag (session_id, tag) VALUES (?, ?)
            """, (session['id'], tag))
        
        self.conn.commit()
        
        print(f"✓ Сессия импортирована: {session['id']}")
        print(f"  Название: {session['title']}")
        print(f"  Сообщений: {len(messages)}")
        print(f"  Изменений файлов: {len(file_changes)}")
        print(f"  Тегов: {len(tags)}")
    
    def close(self):
        if self.conn:
            self.conn.close()


def main():
    if not DB_PATH.exists():
        print("❌ База данных не найдена. Запустите init_db.py сначала.")
        sys.exit(1)
    
    if len(sys.argv) < 2:
        print("Использование:")
        print("  python export_import.py export-json <session_id> <output.json>")
        print("  python export_import.py export-all <output.json>")
        print("  python export_import.py export-md <session_id> <output.md>")
        print("  python export_import.py export-prompt <session_id> <output.txt>")
        print("  python export_import.py import-json <input.json>")
        sys.exit(0)
    
    command = sys.argv[1]
    
    if command == "export-json":
        if len(sys.argv) < 4:
            print("❌ Укажите session_id и output файл")
            sys.exit(1)
        exporter = ContextExporter()
        exporter.export_session_to_json(sys.argv[2], sys.argv[3])
        exporter.close()
    
    elif command == "export-all":
        if len(sys.argv) < 3:
            print("❌ Укажите output файл")
            sys.exit(1)
        exporter = ContextExporter()
        exporter.export_all_sessions_to_json(sys.argv[2])
        exporter.close()
    
    elif command == "export-md":
        if len(sys.argv) < 4:
            print("❌ Укажите session_id и output файл")
            sys.exit(1)
        exporter = ContextExporter()
        exporter.export_to_markdown(sys.argv[2], sys.argv[3])
        exporter.close()
    
    elif command == "export-prompt":
        if len(sys.argv) < 4:
            print("❌ Укажите session_id и output файл")
            sys.exit(1)
        exporter = ContextExporter()
        exporter.export_for_ai_prompt(sys.argv[2], sys.argv[3])
        exporter.close()
    
    elif command == "import-json":
        if len(sys.argv) < 3:
            print("❌ Укажите input файл")
            sys.exit(1)
        importer = ContextImporter()
        importer.import_from_json(sys.argv[2])
        importer.close()
    
    else:
        print(f"❌ Неизвестная команда: {command}")
        sys.exit(1)

if __name__ == "__main__":
    main()
