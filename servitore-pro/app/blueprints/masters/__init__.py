from flask import Blueprint
masters_bp = Blueprint('masters', __name__)
from app.blueprints.masters import routes  # noqa
