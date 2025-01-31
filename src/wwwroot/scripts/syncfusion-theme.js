export function setTheme(theme, appRootPath) {
    var body = document.getElementsByTagName('body')[0];
    body.style.display = 'none';
    body.className = theme;
    document.getElementById('syncfusion-theme').href = `${appRootPath}/_content/Syncfusion.Blazor.Themes/${theme}.css`;
    setTimeout(function () {
        body.style.display = '';
    }, 500);
}
