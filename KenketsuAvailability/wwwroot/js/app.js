window.kenketsu = {
    /**
     * チェックボックスの「一部選択」状態は属性ではなくDOMプロパティなので、
     * Blazor のレンダリングでは表現できない。描画のたびにまとめて反映する。
     */
    syncIndeterminate: function () {
        document.querySelectorAll('input[type=checkbox][data-indeterminate]').forEach(function (el) {
            el.indeterminate = el.dataset.indeterminate === 'true';
        });
    },

    /** モーダルを開いた直後に入力欄へフォーカスを移す。 */
    focus: function (id) {
        const el = document.getElementById(id);
        if (el) el.focus();
    },

    /** Escape でモーダルを閉じる。document 全体で拾う必要があるのでJS側に置く。 */
    registerEscape: function (dotNetRef) {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                dotNetRef.invokeMethodAsync('OnEscape');
            }
        });
    }
};
