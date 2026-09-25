import { api, setSession } from './api.js';
import { h, openDialog, field, errorBox, showError, withBusy, tabs, toast } from './ui.js';

/** Диалог входа/регистрации. Promise<boolean>: true, если пользователь вошёл. */
export function openAuthDialog(mode = 'login') {
  return new Promise((resolve) => {
    let loggedIn = false;
    const values = { fullName: '', email: '' };
    const body = h('div');
    const dialog = openDialog('Вход в RoomBook', body);
    dialog.addEventListener('close', () => resolve(loggedIn));
    render(mode);

    function render(current) {
      const isLogin = current === 'login';
      const error = errorBox();
      const fullName = h('input', { name: 'fullName', required: true, maxlength: 200, autocomplete: 'name', placeholder: 'Иван Петров', value: values.fullName });
      const email = h('input', { name: 'email', type: 'email', required: true, autocomplete: 'email', placeholder: 'you@example.com', value: values.email });
      const password = h('input', {
        name: 'password',
        type: 'password',
        required: true,
        minlength: isLogin ? null : 6,
        autocomplete: isLogin ? 'current-password' : 'new-password',
      });
      const submit = h('button', { class: 'btn btn--primary btn--block', type: 'submit' }, isLogin ? 'Войти' : 'Создать аккаунт');

      const switchTo = (next) => {
        values.fullName = fullName.value;
        values.email = email.value;
        render(next);
      };

      const form = h('form', {
        class: 'form',
        onsubmit: async (event) => {
          event.preventDefault();
          error.hidden = true;
          await withBusy(submit, async () => {
            try {
              const result = isLogin
                ? await api.login(email.value.trim(), password.value)
                : await api.register(fullName.value.trim(), email.value.trim(), password.value);
              loggedIn = true;
              setSession(result);
              toast(isLogin ? `Здравствуйте, ${result.user.fullName}!` : 'Аккаунт создан, вы вошли в систему.');
              dialog.close();
            } catch (err) {
              showError(error, err);
            }
          });
        },
      },
        tabs([{ id: 'login', label: 'Вход' }, { id: 'register', label: 'Регистрация' }], current, switchTo),
        error,
        !isLogin && field('Имя и фамилия', fullName),
        field('Email', email),
        field('Пароль', password, isLogin ? null : 'Не короче 6 символов'),
        submit,
        !isLogin && h('p', { class: 'form__note' },
          'Новые пользователи получают роль «Пользователь». Права администратора выдаются в базе данных — см. README.'));

      body.replaceChildren(form);
      (isLogin ? email : fullName).focus();
    }
  });
}
