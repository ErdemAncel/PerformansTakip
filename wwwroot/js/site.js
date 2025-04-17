// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Toast mesajları için yardımcı fonksiyon
function showToast(message, type = 'success') {
    const Toast = Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        didOpen: (toast) => {
            toast.addEventListener('mouseenter', Swal.stopTimer)
            toast.addEventListener('mouseleave', Swal.resumeTimer)
        }
    })

    Toast.fire({
        icon: type,
        title: message
    })
}

// Mobil cihazlar için puan girişi
function showScoreInput(studentId, studentName, currentScore) {
    // Mobil cihaz kontrolü
    const isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    
    if (isMobile) {
        // Mobil için özel modal
        Swal.fire({
            title: `${studentName} - Puan Girişi`,
            html: `
                <div class="score-input-container">
                    <input type="number" 
                           class="form-control score-input" 
                           value="${currentScore || ''}" 
                           min="0" 
                           max="100" 
                           placeholder="0-100 arası puan girin"
                           autofocus>
                </div>
            `,
            showCancelButton: true,
            confirmButtonText: 'Kaydet',
            cancelButtonText: 'İptal',
            showLoaderOnConfirm: true,
            preConfirm: () => {
                const score = document.querySelector('.score-input').value;
                if (!score || score < 0 || score > 100) {
                    Swal.showValidationMessage('Lütfen 0-100 arası geçerli bir puan girin');
                    return false;
                }
                return score;
            }
        }).then((result) => {
            if (result.isConfirmed) {
                // Puanı kaydet
                saveScore(studentId, result.value);
            }
        });
    } else {
        // Masaüstü için mevcut modal
        $('#scoreModal').modal('show');
        $('#studentId').val(studentId);
        $('#score').val(currentScore || '');
    }
}

// Puan kaydetme fonksiyonu
function saveScore(studentId, score) {
    $.ajax({
        url: '/Student/UpdateScore',
        type: 'POST',
        data: {
            studentId: studentId,
            score: score
        },
        success: function(response) {
            if (response.success) {
                showToast('Puan başarıyla güncellendi', 'success');
                // Tabloyu güncelle
                $(`#score-${studentId}`).text(score);
            } else {
                showToast(response.message || 'Bir hata oluştu', 'error');
            }
        },
        error: function() {
            showToast('Bir hata oluştu', 'error');
        }
    });
}

// Form gönderimlerini yönet
$(document).ready(function() {
    // Sayısal input alanları için özel işlem
    $('input[type="number"]').on('input', function() {
        // Sadece sayısal değerleri kabul et
        this.value = this.value.replace(/[^0-9]/g, '');
        
        // Maksimum değer kontrolü
        const max = parseInt($(this).attr('max'));
        if (max && parseInt(this.value) > max) {
            this.value = max;
        }
        
        // Minimum değer kontrolü
        const min = parseInt($(this).attr('min'));
        if (min && parseInt(this.value) < min) {
            this.value = min;
        }
    });

    // Form submit işlemleri
    $('form').on('submit', function(e) {
        const form = $(this);
        const submitButton = form.find('button[type="submit"]');
        
        // Submit butonunu devre dışı bırak
        submitButton.prop('disabled', true);
        
        // Loading animasyonu ekle
        submitButton.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> İşleniyor...');
    });

    // Modal kapatıldığında formu sıfırla
    $('.modal').on('hidden.bs.modal', function() {
        $(this).find('form')[0].reset();
        $(this).find('button[type="submit"]').prop('disabled', false).html('Kaydet');
    });

    // Tablo sıralama
    $('.sortable').on('click', function() {
        const table = $(this).closest('table');
        const rows = table.find('tr:gt(0)').toArray().sort(comparator($(this).index()));
        this.asc = !this.asc;
        if (!this.asc) {
            rows.reverse();
        }
        for (let i = 0; i < rows.length; i++) {
            table.append(rows[i]);
        }
    });

    function comparator(index) {
        return function(a, b) {
            const valA = getCellValue(a, index);
            const valB = getCellValue(b, index);
            return $.isNumeric(valA) && $.isNumeric(valB) ? 
                valA - valB : valA.toString().localeCompare(valB);
        }
    }

    function getCellValue(row, index) {
        return $(row).children('td').eq(index).text();
    }

    // Responsive tablo için yardımcı fonksiyon
    function responsiveTable() {
        $('.table-responsive').each(function() {
            const table = $(this);
            const wrapper = $('<div class="table-wrapper"></div>');
            table.wrap(wrapper);
        });
    }

    // Sayfa yüklendiğinde responsive tabloları ayarla
    responsiveTable();
    $(window).resize(responsiveTable);

    // Animasyonlu scroll
    $('a[href^="#"]').on('click', function(e) {
        e.preventDefault();
        const target = $(this.hash);
        if (target.length) {
            $('html, body').animate({
                scrollTop: target.offset().top - 70
            }, 800);
        }
    });

    // Form validasyonu
    $.validator.setDefaults({
        errorElement: 'span',
        errorClass: 'invalid-feedback',
        highlight: function(element) {
            $(element).addClass('is-invalid');
        },
        unhighlight: function(element) {
            $(element).removeClass('is-invalid');
        },
        errorPlacement: function(error, element) {
            if (element.parent('.input-group').length) {
                error.insertAfter(element.parent());
            } else {
                error.insertAfter(element);
            }
        }
    });

    // Sayısal input alanları için özel validasyon
    $.validator.addMethod("numberRange", function(value, element, params) {
        const min = params[0];
        const max = params[1];
        const num = parseInt(value);
        return this.optional(element) || (num >= min && num <= max);
    }, "Lütfen {0} ile {1} arasında bir değer girin.");

    // Puan girişi butonlarına tıklama olayı ekle
    $('.score-input-btn').on('click', function() {
        const studentId = $(this).data('student-id');
        const studentName = $(this).data('student-name');
        const currentScore = $(this).data('current-score');
        showScoreInput(studentId, studentName, currentScore);
    });
});
