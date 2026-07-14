from flask import Blueprint
admin_bp = Blueprint('administration', __name__)
from app.blueprints.administration import routes  # noqa
