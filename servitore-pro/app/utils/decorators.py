from functools import wraps
from flask import abort
from flask_login import current_user


def role_required(*roles):
    """Restrict access to users with one of the specified roles."""
    def decorator(f):
        @wraps(f)
        def decorated(*args, **kwargs):
            if not current_user.is_authenticated:
                abort(403)
            if current_user.role not in roles:
                abort(403)
            return f(*args, **kwargs)
        return decorated
    return decorator


def admin_required(f):
    return role_required('Admin')(f)


def manager_or_admin(f):
    return role_required('Admin', 'Manager')(f)
