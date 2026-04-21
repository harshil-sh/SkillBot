window.skillbotInterop = {
    highlightCode: function() {
        if (window.Prism) { Prism.highlightAll(); }
    },
    scrollToBottom: function(elementId) {
        const el = document.getElementById(elementId);
        if (el) el.scrollTop = el.scrollHeight;
    },
    copyToClipboard: function(text) {
        return navigator.clipboard.writeText(text);
    },
    registerKeyboardShortcuts: function(dotnetRef) {
        document.addEventListener('keydown', function(e) {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') { e.preventDefault(); dotnetRef.invokeMethodAsync('FocusSearch'); }
            if ((e.ctrlKey || e.metaKey) && e.key === 'n') { e.preventDefault(); dotnetRef.invokeMethodAsync('NewConversation'); }
            if (e.key === 'Escape') { dotnetRef.invokeMethodAsync('CloseDialogs'); }
        });
    }
};
