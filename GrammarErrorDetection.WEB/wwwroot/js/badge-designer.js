// Solo recibe el SVG autónomo generado por el catálogo local; no carga imágenes externas.
export async function renderPng(svg) {
    const url = URL.createObjectURL(new Blob([svg], { type: "image/svg+xml;charset=utf-8" }));
    try {
        const image = new Image();
        await new Promise((resolve, reject) => {
            image.onload = resolve;
            image.onerror = () => reject(new Error("No se pudo preparar la imagen del badge."));
            image.src = url;
        });
        const canvas = document.createElement("canvas");
        canvas.width = 512;
        canvas.height = 512;
        const context = canvas.getContext("2d");
        if (!context) throw new Error("No se pudo preparar el editor de imagen.");
        context.clearRect(0, 0, 512, 512);
        context.drawImage(image, 0, 0, 512, 512);
        const blob = await new Promise((resolve, reject) => canvas.toBlob(
            value => value ? resolve(value) : reject(new Error("No se pudo exportar la imagen.")), "image/png"));
        if (blob.size > 5 * 1024 * 1024) throw new Error("La imagen supera el tamaño permitido.");
        return new Uint8Array(await blob.arrayBuffer());
    } finally {
        URL.revokeObjectURL(url);
    }
}
