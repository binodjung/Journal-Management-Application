let quill;

window.initQuill = (editorId) => {
    quill = new Quill(`#${editorId}`, {
        theme: "snow",
        modules: {
            toolbar: [
                ["bold", "italic", "underline"],
                [{ list: "ordered" }, { list: "bullet" }],
                [{ header: [1, 2, false] }],
                ["clean"]
            ]
        }
    });
};

window.getQuillHtml = () => {
    return quill ? quill.root.innerHTML : "";
};

window.setQuillHtml = (html) => {
    if (quill) {
        quill.root.innerHTML = html;
    }
};

window.clearQuillEditor = () => {
    if (quill) {
        quill.setText("");
    }
};