#!/usr/bin/env python3
"""
AI Memory System - Session Logger
Логирует все взаимодействия с AI в базу данных
"""

import sqlite3
import json
import uuid
from datetime import datetime
from pathlib import Path
from typing import Optional, Dict, List

DB_PATH = Path(__file__).parent / "memory.db"

class SessionLogger:
    def __init__(self, project_id: str = "proj_stilak_main"):
        self.project_id = project_id
        self.session_id = None
        self.conn = None
        
    def start_session(self, title: str = "Новая сессия", parent_id: Optional[str] = None) -> str:
        """Начать новую сессию"""
        self.conn = sqlite3.connect(DB_PATH)
        self.session_id = f"ses_{uuid.uuid4().hex[:12]}"
        timestamp = int(datetime.now().timestamp() * 1000)
        
        cursor = self.conn.cursor()
        cursor.execute("""
            INSERT INTO session (id, project_id, parent_id, title, time_created, time_updated)
            VALUES (?, ?, ?, ?, ?, ?)
        """, (self.session_id, self.project_id, parent_id, title, timestamp, timestamp))
        
        self.conn.commit()
        print(f"✓ Сессия начата: {self.session_id}")
        return self.session_id
    
    def log_message(self, role: str, content: str, metadata: Optional[Dict] = None) -> str:
        """Логировать сообщение (user или assistant)"""
        if not self.session_id:
            raise Exception("Сессия не начата. Вызовите start_session() сначала.")
        
        message_id = f"msg_{uuid.uuid4().hex[:12]}"
        timestamp = int(datetime.now().timestamp() * 1000)
        
        cursor = self.conn.cursor()
        cursor.execute("""
            INSERT INTO message (id, session_id, role, content, metadata, time_created)
            VALUES (?, ?, ?, ?, ?, ?)
        """, (
            message_id,
            self.session_id,
            role,
            content,
            json.dumps(metadata) if metadata else None,
            timestamp
        ))
        
        # Обновляем время последнего обновления сессии
        cursor.execute("""
            UPDATE session SET time_updated = ? WHERE id = ?
        """, (timestamp, self.session_id))
        
        self.conn.commit()
        return message_id
    
    def log_file_change(self, file_path: str, action: str, 
                       content_before: Optional[str] = None,
                       content_after: Optional[str] = None):
        """Логировать изменение файла"""
        if not self.session_id:
            raise Exception("Сессия не начата.")
        
        timestamp = int(datetime.now().timestamp() * 1000)
        
        cursor = self.conn.cursor()
        cursor.execute("""
            INSERT INTO file_change (session_id, file_path, action, content_before, content_after, time_created)
            VALUES (?, ?, ?, ?, ?, ?)
        """, (self.session_id, file_path, action, content_before, content_after, timestamp))
        
        self.conn.commit()
    
    def add_tags(self, tags: List[str]):
        """Добавить теги к текущей сессии"""
        if not self.session_id:
            raise Exception("Сессия не начата.")
        
        cursor = self.conn.cursor()
        for tag in tags:
            cursor.execute("""
                INSERT INTO tag (session_id, tag) VALUES (?, ?)
            """, (self.session_id, tag))
        
        self.conn.commit()
    
    def update_summary(self, summary: str):
        """Обновить краткое описание сессии"""
        if not self.session_id:
            raise Exception("Сессия не начата.")
        
        cursor = self.conn.cursor()
        cursor.execute("""
            UPDATE session SET summary = ? WHERE id = ?
        """, (summary, self.session_id))
        
        self.conn.commit()
    
    def end_session(self):
        """Завершить сессию"""
        if self.conn:
            self.conn.close()
            print(f"✓ Сессия завершена: {self.session_id}")
            self.session_id = None
            self.conn = None
    
    def __enter__(self):
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        self.end_session()


class MemoryReader:
    """Класс для чтения истории из базы данных"""
    
    def __init__(self):
        self.conn = sqlite3.connect(DB_PATH)
        self.conn.row_factory = sqlite3.Row
    
    def get_recent_sessions(self, limit: int = 10) -> List[Dict]:
        """Получить последние сессии"""
        cursor = self.conn.cursor()
        cursor.execute("""
            SELECT 
                s.id,
                s.title,
                s.summary,
                datetime(s.time_updated/1000, 'unixepoch', 'localtime') as updated,
                (SELECT COUNT(*) FROM message m WHERE m.session_id = s.id) as msg_count
            FROM session s
            WHERE s.parent_id IS NULL
            ORDER BY s.time_updated DESC
            LIMIT ?
        """, (limit,))
        
        return [dict(row) for row in cursor.fetchall()]
    
    def get_session_messages(self, session_id: str) -> List[Dict]:
        """Получить все сообщения из сессии"""
        cursor = self.conn.cursor()
        cursor.execute("""
            SELECT 
                role,
                content,
                metadata,
                datetime(time_created/1000, 'unixepoch', 'localtime') as created
            FROM message
            WHERE session_id = ?
            ORDER BY time_created ASC
        """, (session_id,))
        
        messages = []
        for row in cursor.fetchall():
            msg = dict(row)
            if msg['metadata']:
                msg['metadata'] = json.loads(msg['metadata'])
            messages.append(msg)
        
        return messages
    
    def search_messages(self, query: str, limit: int = 20) -> List[Dict]:
        """Поиск по содержимому сообщений"""
        cursor = self.conn.cursor()
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
        
        return [dict(row) for row in cursor.fetchall()]
    
    def get_file_changes(self, session_id: str) -> List[Dict]:
        """Получить все изменения файлов в сессии"""
        cursor = self.conn.cursor()
        cursor.execute("""
            SELECT 
                file_path,
                action,
                datetime(time_created/1000, 'unixepoch', 'localtime') as created
            FROM file_change
            WHERE session_id = ?
            ORDER BY time_created ASC
        """, (session_id,))
        
        return [dict(row) for row in cursor.fetchall()]
    
    def get_ai_config(self) -> Dict[str, str]:
        """Получить конфигурацию AI"""
        cursor = self.conn.cursor()
        cursor.execute("SELECT key, value FROM ai_config")
        
        return {row['key']: row['value'] for row in cursor.fetchall()}
    
    def close(self):
        if self.conn:
            self.conn.close()
    
    def __enter__(self):
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()


# Пример использования
if __name__ == "__main__":
    # Создаем новую сессию
    with SessionLogger() as logger:
        logger.start_session("Тестовая сессия")
        
        # Логируем сообщения
        logger.log_message("user", "Привет, помоги мне с кодом")
        logger.log_message("assistant", "Конечно, чем могу помочь?")
        
        # Добавляем теги
        logger.add_tags(["coding", "python", "test"])
        
        # Логируем изменение файла
        logger.log_file_change(
            "test.py",
            "created",
            content_after="print('Hello World')"
        )
        
        # Обновляем описание
        logger.update_summary("Тестовая сессия для проверки логирования")
    
    # Читаем историю
    with MemoryReader() as reader:
        print("\n=== Последние сессии ===")
        sessions = reader.get_recent_sessions(5)
        for s in sessions:
            print(f"{s['id']}: {s['title']} ({s['msg_count']} сообщений)")
        
        print("\n=== Конфигурация AI ===")
        config = reader.get_ai_config()
        for key, value in config.items():
            print(f"{key}: {value}")
