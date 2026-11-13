// URL temporal (blob:) del archivo elegido en un <input type="file">, para previsualizarlo antes de subirlo.
export function createUrl(input) {
    const file = input && input.files && input.files[0];
    return file ? URL.createObjectURL(file) : null;
}

export function revokeUrl(url) {
    if (url) URL.revokeObjectURL(url);
}
