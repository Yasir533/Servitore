from flask import render_template, redirect, url_for, flash, request
from flask_login import login_required, current_user
from datetime import datetime
from app.blueprints.administration import admin_bp
from app.extensions import db
from app.models.user import User
from app.utils.decorators import admin_required


@admin_bp.route('/')
@login_required
@admin_required
def index():
    users = User.query.order_by(User.created_at.desc()).all()
    return render_template('administration/index.html', users=users)


@admin_bp.route('/users')
@login_required
@admin_required
def list_users():
    users = User.query.order_by(User.full_name).all()
    return render_template('administration/users.html', users=users)


@admin_bp.route('/users/add', methods=['GET', 'POST'])
@login_required
@admin_required
def add_user():
    if request.method == 'POST':
        username = request.form.get('username', '').strip()
        email = request.form.get('email', '').strip()
        password = request.form.get('password', '')
        if User.query.filter_by(username=username).first():
            flash('Username already exists.', 'danger')
        elif User.query.filter_by(email=email).first():
            flash('Email already exists.', 'danger')
        elif len(password) < 6:
            flash('Password must be at least 6 characters.', 'danger')
        else:
            u = User(
                username=username, email=email,
                full_name=request.form.get('full_name', '').strip(),
                role=request.form.get('role', 'Viewer'),
                phone=request.form.get('phone', '').strip(),
            )
            u.set_password(password)
            db.session.add(u)
            db.session.commit()
            flash(f'User "{username}" created.', 'success')
            return redirect(url_for('administration.list_users'))
    return render_template('administration/user_form.html', user=None)


@admin_bp.route('/users/<int:id>/edit', methods=['GET', 'POST'])
@login_required
@admin_required
def edit_user(id):
    u = User.query.get_or_404(id)
    if request.method == 'POST':
        u.full_name = request.form.get('full_name', u.full_name).strip()
        u.role = request.form.get('role', u.role)
        u.phone = request.form.get('phone', '').strip()
        u.is_active = request.form.get('is_active') == 'on'
        new_password = request.form.get('new_password', '')
        if new_password and len(new_password) >= 6:
            u.set_password(new_password)
        db.session.commit()
        flash(f'User "{u.username}" updated.', 'success')
        return redirect(url_for('administration.list_users'))
    return render_template('administration/user_form.html', user=u)


@admin_bp.route('/settings', methods=['GET', 'POST'])
@login_required
@admin_required
def settings():
    if request.method == 'POST':
        flash('Settings saved.', 'success')
        return redirect(url_for('administration.settings'))
    return render_template('administration/settings.html')


@admin_bp.route('/about')
@login_required
def about():
    user_count = User.query.count()
    from app.models.service_call import ServiceCall
    call_count = ServiceCall.query.count()
    from app.models.customer import Customer
    customer_count = Customer.query.count()
    return render_template('administration/about.html',
                           user_count=user_count, call_count=call_count,
                           customer_count=customer_count)
