// ShopRecorder — UI helpers (modals, toasts, printing, PDF download)
(function () {
    'use strict';

    window.ui = {
        showModal: function (id) {
            var el = document.getElementById(id);
            if (el) bootstrap.Modal.getOrCreateInstance(el).show();
        },

        hideModal: function (id) {
            var el = document.getElementById(id);
            if (el) {
                var inst = bootstrap.Modal.getInstance(el);
                if (inst) inst.hide(); else bootstrap.Modal.getOrCreateInstance(el).hide();
            }
        },

        print: function () { window.print(); },

        toast: function (message, type) {
            type = type || 'success';
            var host = document.getElementById('toastHost');
            if (!host) { alert(message); return; }

            var icons = { success: 'check-circle-fill', error: 'exclamation-triangle-fill', info: 'info-circle-fill' };

            var el = document.createElement('div');
            el.className = 'app-toast';
            el.setAttribute('role', 'status');

            var ic = document.createElement('span');
            ic.className = 't-ic t-' + type;
            var icon = document.createElement('i');
            icon.className = 'bi bi-' + (icons[type] || 'bell-fill');
            ic.appendChild(icon);

            var txt = document.createElement('span');
            txt.textContent = message; // textContent → safe for any product name

            el.appendChild(ic);
            el.appendChild(txt);
            host.appendChild(el);

            if (window.bootstrap && bootstrap.Toast) {
                var t = new bootstrap.Toast(el, { delay: 3200 });
                t.show();
                el.addEventListener('hidden.bs.toast', function () { el.remove(); });
            } else {
                setTimeout(function () { el.remove(); }, 3200);
            }
        }
    };

    // ---- Receipt PDF download (jsPDF + html2canvas, loaded from CDN) ----
    window.downloadReceiptPdf = async function (elementId, fileName) {
        var el = document.getElementById(elementId);
        if (!el) return;

        // Graceful fallback: if CDN libs are unavailable, open the print dialog
        // so the user can still "Save as PDF" from the browser.
        if (!window.jspdf || !window.html2canvas) { window.print(); return; }

        var canvas = await html2canvas(el, { scale: 3, backgroundColor: '#ffffff' });
        var widthMm = 58;
        var heightMm = (canvas.height / canvas.width) * (widthMm - 10) + 10;

        var jsPDF = window.jspdf.jsPDF;
        var pdf = new jsPDF({ orientation: 'portrait', unit: 'mm', format: [widthMm, heightMm] });
        pdf.addImage(canvas.toDataURL('image/png'), 'PNG', 5, 5, widthMm - 10, heightMm - 10);
        pdf.save(fileName);
    };
})();
