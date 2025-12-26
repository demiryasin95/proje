// Notes Page JavaScript

let simplemde; let currentNoteId = null;
let viewingNoteId = null;
let allNotes = [];

$(document).ready(function () {
    loadCategories();
    loadNotes();

    // Search functionality
    $('#searchInput').on('input', function () {
        filterNotes();
    });

    // Category filter
    $('#categorySelect').on('change', function () {
        filterNotes();
    });

    // Delete confirmation
    $('#confirmDeleteBtn').on('click', function () {
        if (currentNoteId) {
            deleteNote(currentNoteId);
        }
    });
});

function loadCategories() {
    $.get('/Notes/GetCategories', function (data) {
        if (data.success && data.categories) {
            const categorySelect = $('#categorySelect');
            const noteCategorySelect = $('#noteCategory');

            // Clear existing options except the first one
            categorySelect.find('option:not(:first)').remove();
            noteCategorySelect.find('option:not(:first)').remove();

            data.categories.forEach(function (category) {
                categorySelect.append(`<option value="${category}">${category}</option>`);
                noteCategorySelect.append(`<option value="${category}">${category}</option>`);
            });
        }
    });
}

function loadNotes(category = '') {
    $.get('/Notes/GetNotes', { category: category }, function (data) {
        if (data.success) {
            allNotes = data.notes || [];
            displayNotes(allNotes);
        }
    });
}

// Parse ASP.NET JSON date format (/Date(milliseconds)/)
function parseDotNetDate(dotNetDate) {
    if (!dotNetDate) return new Date();

    // Check if it's the ASP.NET JSON format
    const match = /\/Date\((\d+)\)\//.exec(dotNetDate);
    if (match) {
        return new Date(parseInt(match[1]));
    }

    // Fallback to standard date parsing
    return new Date(dotNetDate);
}

function displayNotes(notes) {
    const container = $('#notesContainer');

    if (!notes || notes.length === 0) {
        container.html(`
            <div class="col-12 text-center text-muted py-5">
                <i class="bi bi-journal-x" style="font-size: 4rem; opacity: 0.3;"></i>
                <p class="mt-3">Henüz not eklenmemiş. Yeni not eklemek için yukarıdaki butonu kullanın.</p>
            </div>
        `);
        return;
    }

    let html = '';
    notes.forEach(function (note) {
        const createdDate = parseDotNetDate(note.createdAt).toLocaleDateString('tr-TR');
        const updatedDate = parseDotNetDate(note.updatedAt).toLocaleDateString('tr-TR');

        html += `
            <div class="col-md-6 col-lg-4">
                <div class="note-card" onclick="viewNote(${note.id})">
                    <div class="note-card-header">
                        <div style="flex: 1;">
                            <h3 class="note-card-title">${escapeHtml(note.title)}</h3>
                            <div class="note-card-meta">
                                ${note.category ? `<span class="note-category-badge">${escapeHtml(note.category)}</span>` : ''}
                                <span class="note-date">
                                    <i class="bi bi-clock"></i>
                                    ${updatedDate}
                                </span>
                            </div>
                        </div>
                    </div>
                    <div class="note-content-preview">
                        ${escapeHtml(note.shortContent)}
                    </div>
                    <div class="note-card-actions" onclick="event.stopPropagation();">
                        <button class="btn btn-sm btn-outline-primary" onclick="editNote(${note.id})">
                            <i class="bi bi-pencil"></i> Düzenle
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="confirmDelete(${note.id})">
                            <i class="bi bi-trash"></i> Sil
                        </button>
                    </div>
                </div>
            </div>
        `;
    });

    container.html(html);
}

function filterNotes() {
    const searchTerm = $('#searchInput').val().toLowerCase();
    const selectedCategory = $('#categorySelect').val();

    let filtered = allNotes;

    if (selectedCategory) {
        filtered = filtered.filter(note => note.category === selectedCategory);
    }

    if (searchTerm) {
        filtered = filtered.filter(note =>
            note.title.toLowerCase().includes(searchTerm) ||
            note.content.toLowerCase().includes(searchTerm) ||
            (note.category && note.category.toLowerCase().includes(searchTerm))
        );
    }

    displayNotes(filtered);
}

function openNoteModal(noteId = null) {
    currentNoteId = noteId;

    // Initialize SimpleMDE if not already initialized
    if (!simplemde) {
        simplemde = new SimpleMDE({
            element: document.getElementById('noteContent'),
            spellChecker: false,
            placeholder: 'Notunuzu buraya yazın...\n\n# Markdown kullanabilirsiniz\n- Liste öğesi\n- **Kalın metin**\n- *İtalik metin*',
            status: false,
            toolbar: [
                'bold', 'italic', 'heading', '|',
                'quote', 'unordered-list', 'ordered-list', '|',
                'link', 'image', '|',
                'preview', 'side-by-side', 'fullscreen', '|',
                'guide'
            ]
        });
    }

    if (noteId) {
        // Edit mode
        $('#modalTitle').text('Notu Düzenle');
        const note = allNotes.find(n => n.id === noteId);
        if (note) {
            $('#noteId').val(note.id);
            $('#noteTitle').val(note.title);
            $('#noteCategory').val(note.category);
            simplemde.value(note.content);
        }
    } else {
        // New note mode
        $('#modalTitle').text('Yeni Not');
        $('#noteId').val('');
        $('#noteTitle').val('');
        $('#noteCategory').val('');
        simplemde.value('');
    }

    // Show editor, hide preview
    simplemde.codemirror.refresh();
    $('#preview').hide();
    $('#previewBtnText').text('Önizleme');

    $('#noteModal').modal('show');
}

function togglePreview() {
    const preview = $('#preview');
    const previewBtn = $('#previewBtnText');

    if (preview.is(':visible')) {
        // Switch to editor
        preview.hide();
        simplemde.codemirror.refresh();
        previewBtn.text('Önizleme');
    } else {
        // Switch to preview
        const markdown = simplemde.value();
        const html = marked.parse(markdown);
        preview.html(html);
        preview.show();
        previewBtn.text('Düzenle');
    }
}

function saveNote() {
    const noteId = $('#noteId').val();
    const title = $('#noteTitle').val().trim();
    const content = simplemde.value().trim();
    const category = $('#noteCategory').val();

    if (!title) {
        alert('Lütfen bir başlık girin!');
        return;
    }

    if (!content) {
        alert('Lütfen not içeriği girin!');
        return;
    }

    if (!category) {
        alert('Lütfen bir ders seçin!');
        return;
    }

    const url = noteId ? '/Notes/UpdateNote' : '/Notes/CreateNote';
    const data = {
        id: noteId || 0,
        title: title,
        content: content,
        category: category
    };

    $.ajax({
        url: url,
        type: 'POST',
        headers: {
            'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val()
        },
        data: data,
        success: function (response) {
            if (response.success) {
                $('#noteModal').modal('hide');
                showNotification('success', response.message);
                loadNotes();
                loadCategories();
            } else {
                showNotification('danger', response.message);
            }
        },
        error: function (xhr, status, error) {
            showNotification('danger', 'Bir hata oluştu: ' + error);
        }
    });
}

function editNote(noteId) {
    openNoteModal(noteId);
}

function editNoteFromView() {
    $('#viewNoteModal').modal('hide');
    setTimeout(() => {
        openNoteModal(viewingNoteId);
    }, 300);
}

function viewNote(noteId) {
    viewingNoteId = noteId;
    const note = allNotes.find(n => n.id === noteId);

    if (!note) return;

    $('#viewNoteTitle').text(note.title);
    $('#viewNoteCategory').text(note.category || 'Genel');

    const createdDate = parseDotNetDate(note.createdAt).toLocaleDateString('tr-TR', {
        year: 'numeric', month: 'long', day: 'numeric'
    });
    const updatedDate = parseDotNetDate(note.updatedAt).toLocaleDateString('tr-TR', {
        year: 'numeric', month: 'long', day: 'numeric'
    });

    const dateText = createdDate === updatedDate
        ? `Oluşturulma: ${createdDate}`
        : `Oluşturulma: ${createdDate} • Güncelleme: ${updatedDate}`;

    $('#viewNoteDates').text(dateText);

    const html = marked.parse(note.content);
    $('#viewNoteContent').html(html);

    $('#viewNoteModal').modal('show');
}

function confirmDelete(noteId) {
    currentNoteId = noteId;
    $('#deleteModal').modal('show');
}

function deleteNote(noteId) {
    $.ajax({
        url: '/Notes/DeleteNote',
        type: 'POST',
        headers: {
            'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val()
        },
        data: { id: noteId },
        success: function (response) {
            if (response.success) {
                $('#deleteModal').modal('hide');
                showNotification('success', response.message);
                loadNotes();
                currentNoteId = null;
            } else {
                showNotification('danger', response.message);
            }
        },
        error: function (xhr, status, error) {
            showNotification('danger', 'Bir hata oluştu: ' + error);
        }
    });
}

function showNotification(type, message) {
    const bgClass = type === 'success' ? 'bg-success text-white' : 'bg-danger text-white';
    const headerTitle = type === 'success' ? 'Başarılı' : 'Hata';

    const toastHtml = `
        <div class="toast ${bgClass}" role="alert" data-bs-delay="3000">
            <div class="toast-header ${bgClass}">
                <strong class="me-auto">${headerTitle}</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">${message}</div>
        </div>
    `;

    let container = $('#toastContainer');
    if (container.length === 0) {
        $('body').append('<div id="toastContainer" class="toast-container position-fixed top-0 end-0 p-3"></div>');
        container = $('#toastContainer');
    }

    container.append(toastHtml);
    const toastElement = container.find('.toast').last()[0];
    const toast = new bootstrap.Toast(toastElement);
    toast.show();

    setTimeout(() => {
        $(toastElement).remove();
    }, 3500);
}

function escapeHtml(text) {
    if (!text) return '';
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}
