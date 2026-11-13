export async function downloadReport(filename, contentType, stream) {
    const content = await stream.arrayBuffer();
    const url = URL.createObjectURL(new Blob([content], { type: contentType }));
    const link = document.createElement("a");
    try {
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
    } finally {
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
}
