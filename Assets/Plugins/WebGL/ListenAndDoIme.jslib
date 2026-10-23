var ListenAndDoIme = {
  ListenAndDo_ShowImeInput: function (titlePtr, initialValuePtr, characterLimit) {
    var title = UTF8ToString(titlePtr);
    var initialValue = UTF8ToString(initialValuePtr);
    var overlay = document.getElementById("listenAndDoImeOverlay");

    if (!overlay) {
      var style = document.createElement("style");
      style.id = "listenAndDoImeStyle";
      style.textContent =
        "#listenAndDoImeOverlay{position:fixed;inset:0;z-index:2147483647;display:none;align-items:center;justify-content:center;background:rgba(0,0,0,.55);font-family:Arial,sans-serif}" +
        "#listenAndDoImeCard{box-sizing:border-box;width:min(92vw,440px);padding:24px;border-radius:16px;background:#fff8df;box-shadow:0 8px 28px rgba(0,0,0,.35);text-align:center}" +
        "#listenAndDoImeTitle{margin:0 0 16px;color:#49350d;font-size:20px;font-weight:700}" +
        "#listenAndDoImeInput{box-sizing:border-box;width:100%;height:48px;padding:8px 12px;border:2px solid #d6a92e;border-radius:8px;background:#fff;color:#222;font-size:20px;outline:none}" +
        "#listenAndDoImeButtons{display:flex;justify-content:center;gap:12px;margin-top:18px}" +
        "#listenAndDoImeButtons button{min-width:96px;padding:10px 18px;border:0;border-radius:8px;background:#f2c94c;color:#382900;font-size:17px;font-weight:700;cursor:pointer}" +
        "#listenAndDoImeButtons button:last-child{background:#e4e0d4}";
      document.head.appendChild(style);

      overlay = document.createElement("div");
      overlay.id = "listenAndDoImeOverlay";
      overlay.innerHTML =
        '<div id="listenAndDoImeCard" role="dialog" aria-modal="true">' +
        '<div id="listenAndDoImeTitle"></div>' +
        '<input id="listenAndDoImeInput" type="text" autocomplete="off" />' +
        '<div id="listenAndDoImeButtons"><button id="listenAndDoImeCancel" type="button">キャンセル</button>' +
        '<button id="listenAndDoImeOk" type="button">OK</button></div></div>';
      document.body.appendChild(overlay);

      overlay._listenAndDoValue = "";
      overlay._listenAndDoCanceled = 0;
      overlay._listenAndDoActive = 0;
      overlay._listenAndDoFinish = function (canceled) {
        var field = document.getElementById("listenAndDoImeInput");
        overlay._listenAndDoValue = field ? field.value : "";
        overlay._listenAndDoCanceled = canceled ? 1 : 0;
        overlay._listenAndDoActive = 0;
        overlay.style.display = "none";
      };
      document.getElementById("listenAndDoImeCancel").addEventListener("click", function () {
        overlay._listenAndDoFinish(true);
      });
      document.getElementById("listenAndDoImeOk").addEventListener("click", function () {
        overlay._listenAndDoFinish(false);
      });
      document.getElementById("listenAndDoImeInput").addEventListener("keydown", function (event) {
        if (event.key === "Enter") {
          event.preventDefault();
          overlay._listenAndDoFinish(false);
        } else if (event.key === "Escape") {
          event.preventDefault();
          overlay._listenAndDoFinish(true);
        }
      });
    }

    var titleElement = document.getElementById("listenAndDoImeTitle");
    var input = document.getElementById("listenAndDoImeInput");
    titleElement.textContent = title;
    input.value = initialValue;
    input.maxLength = characterLimit > 0 ? characterLimit : 100;
    overlay._listenAndDoValue = initialValue;
    overlay._listenAndDoCanceled = 0;
    overlay._listenAndDoActive = 1;
    overlay.style.display = "flex";
    window.setTimeout(function () {
      input.focus();
      input.select();
    }, 0);
  },

  ListenAndDo_IsImeInputActive: function () {
    var overlay = document.getElementById("listenAndDoImeOverlay");
    return overlay && overlay._listenAndDoActive ? 1 : 0;
  },

  ListenAndDo_IsImeInputCanceled: function () {
    var overlay = document.getElementById("listenAndDoImeOverlay");
    return overlay && overlay._listenAndDoCanceled ? 1 : 0;
  },

  ListenAndDo_CopyImeInputValue: function (buffer, capacity) {
    var overlay = document.getElementById("listenAndDoImeOverlay");
    var value = overlay ? overlay._listenAndDoValue : "";
    stringToUTF8(value, buffer, capacity);
    return lengthBytesUTF8(value);
  }
};

mergeInto(LibraryManager.library, ListenAndDoIme);
