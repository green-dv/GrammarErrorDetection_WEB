export function show(dialog) {
    if (dialog.isConnected && !dialog.open) dialog.showModal();
}

export function close(dialog) {
    if (dialog.open) dialog.close();
}
