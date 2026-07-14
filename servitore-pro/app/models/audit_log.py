from app.extensions import db
from datetime import datetime


class AuditLog(db.Model):
    __tablename__ = 'audit_logs'
    id = db.Column(db.Integer, primary_key=True)
    user_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    action = db.Column(db.String(100), nullable=False)
    entity = db.Column(db.String(100), nullable=True)
    entity_id = db.Column(db.Integer, nullable=True)
    description = db.Column(db.Text, nullable=True)
    ip_address = db.Column(db.String(50), nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    user = db.relationship('User', foreign_keys=[user_id], lazy='select')

    @staticmethod
    def log(action, entity=None, entity_id=None, description=None, user_id=None, ip=None):
        entry = AuditLog(
            user_id=user_id, action=action, entity=entity,
            entity_id=entity_id, description=description, ip_address=ip
        )
        db.session.add(entry)
        # Don't commit here — caller handles commit

    def __repr__(self):
        return f'<AuditLog {self.action} {self.entity}>'
