function bannerInit(elementId, style) {
    if (window.addEventListener) {
        window.addEventListener('load', function () { krakeBanner(elementId, style); }, false); // NB **not** 'onload'
    }
    else if (window.attachEvent) // Microsoft
    {
        window.attachEvent('onload', function () { krakeBanner(elementId, style); });
    }
}

function krakeBanner(elementId, style) {
    var googlePlayUrl = "https://play.google.com/store/apps/details?id=com.lasergroup.ami";
    var appleStoreUrl = "https://itunes.apple.com/it/app/visitami/id667357331";
    var siteUrl = "//www.anfiteatromorenicoivrea.it";
    var scriptsPath = siteUrl + "/themes/anfiteatromorenico/scripts/";
    var stylePath = siteUrl + "/themes/anfiteatromorenico/styles/";
    var styleImagesPath = stylePath + "images/banner/";

    var head = document.getElementsByTagName('head')[0];
    var container = document.getElementById(elementId);
    if (head != null && container != null) {
        var s = document.createElement('link');
        s.setAttribute('type', 'text/css');
        s.setAttribute('rel', 'stylesheet');
        s.setAttribute('href', stylePath + 'banner.css');
        head.appendChild(s);
        container.setAttribute('class', 'krakebanner ' + style);
        container.innerHTML = '<div class="krakebanner-container"><div class="krakebanner-background"><div class="krakebanner-logo"><a href="' + siteUrl + '"  target="_blank"><img src="' + styleImagesPath + 'logo.png" /></a></div><div class="krakebanner-stores"><a href="' + googlePlayUrl + '" target="_blank"><img src="' + styleImagesPath + 'android.png" class="krakebanner-googleplay" /></a> <a href="' + appleStoreUrl + '"  target="_blank"><img src="' + styleImagesPath + 'apple.png" class="krakebanner-applestore" /></a></div></div></div>';
    }
}
