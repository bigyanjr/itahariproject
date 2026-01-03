// Theme Management
document.addEventListener('DOMContentLoaded', function() {
    // Load saved theme
    const savedTheme = localStorage.getItem('theme') || 'light';
    applyTheme(savedTheme);

    // Theme selector
    document.querySelectorAll('.theme-option').forEach(option => {
        option.addEventListener('click', function(e) {
            e.preventDefault();
            const theme = this.getAttribute('data-theme');
            applyTheme(theme);
            localStorage.setItem('theme', theme);
        });
    });
});

function applyTheme(theme) {
    const html = document.documentElement;
    html.setAttribute('data-theme', theme);
    
    // Update active theme indicator
    document.querySelectorAll('.theme-option').forEach(option => {
        if (option.getAttribute('data-theme') === theme) {
            option.classList.add('active');
        } else {
            option.classList.remove('active');
        }
    });
}

