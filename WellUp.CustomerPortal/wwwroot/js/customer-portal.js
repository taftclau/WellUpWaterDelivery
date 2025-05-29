/* customer-portal.js */
document.addEventListener('DOMContentLoaded', function () {
    // Fade-in animations
    const fadeElements = document.querySelectorAll('.fade-in');

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('show');
            }
        });
    }, { threshold: 0.1 });

    fadeElements.forEach(element => {
        observer.observe(element);
    });

    // Password strength meter
    const passwordInput = document.getElementById('Password');
    const strengthMeter = document.getElementById('password-strength');

    if (passwordInput && strengthMeter) {
        passwordInput.addEventListener('input', function () {
            const password = this.value;
            let strength = 0;

            if (password.length >= 8) strength += 1;
            if (password.match(/[a-z]/) && password.match(/[A-Z]/)) strength += 1;
            if (password.match(/\d/)) strength += 1;
            if (password.match(/[^a-zA-Z\d]/)) strength += 1;

            strengthMeter.className = 'password-strength';

            if (strength === 0) {
                strengthMeter.classList.add('d-none');
            } else if (strength <= 2) {
                strengthMeter.classList.add('password-strength-weak');
                strengthMeter.classList.remove('d-none');
            } else if (strength === 3) {
                strengthMeter.classList.add('password-strength-medium');
                strengthMeter.classList.remove('d-none');
            } else {
                strengthMeter.classList.add('password-strength-strong');
                strengthMeter.classList.remove('d-none');
            }
        });
    }

    // Password confirmation validation
    const confirmPasswordInput = document.getElementById('ConfirmPassword');
    const passwordConfirmFeedback = document.getElementById('password-confirm-feedback');

    if (confirmPasswordInput && passwordInput && passwordConfirmFeedback) {
        confirmPasswordInput.addEventListener('input', function () {
            if (this.value !== passwordInput.value) {
                this.classList.add('is-invalid');
                this.classList.remove('is-valid');
                passwordConfirmFeedback.textContent = "Passwords do not match";
            } else {
                this.classList.remove('is-invalid');
                this.classList.add('is-valid');
                passwordConfirmFeedback.textContent = "";
            }
        });
    }

    // Mobile menu toggle animation
    const navbarToggler = document.querySelector('.navbar-toggler');

    if (navbarToggler) {
        navbarToggler.addEventListener('click', function () {
            this.classList.toggle('open');
        });
    }
});