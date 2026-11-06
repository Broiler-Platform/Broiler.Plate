// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

import { dotnet } from './_framework/dotnet.js';

if (location.origin === 'https://broiler-platform.github.io') {
    const navLink = document.querySelector('.office-nav a');
    if (navLink) navLink.href = '/demo/';
}

const CURSORS = {
    Arrow: 'default', Text: 'text', Hand: 'pointer', Wait: 'wait', Move: 'move',
    ResizeHorizontal: 'ew-resize', ResizeVertical: 'ns-resize',
};

const EDITOR_CTRL_KEYS = new Set(['z', 'y', 'a', 's', 'o', 'n', 'w', 'p']);

let plateExports;
let canvas;
let textInput;
let host;
let statusLine;
let started = false;
let frameRequest = 0;
let frameInProgress = false;
let observedWidth = 1280;
let observedHeight = 800;
let nativeCompositionActive = false;
let compositionEcho = null, compositionEchoTimer;
let framePending = false;
let fileBusy = false;
let filePicker;
let folderPicker;
const listenerCleanups = [];

function prefersDark() { return matchMedia('(prefers-color-scheme: dark)').matches; }
function prefersReducedMotion() { return matchMedia('(prefers-reduced-motion: reduce)').matches; }

function setStatus(text, failed) {
    if (!statusLine) return;
    statusLine.textContent = text;
    if (failed) statusLine.dataset.state = 'failed';
    else delete statusLine.dataset.state;
}

function showError(message) {
    const panel = document.getElementById('error-panel');
    const body = document.getElementById('error');
    if (body) body.textContent = message;
    if (panel) panel.hidden = false;
    setStatus('Managed failure — see details below.', true);
    console.error(message);
}

function reportOperation(outcome, message) {
    const status = document.getElementById('plate-operation');
    if (status) {
        status.dataset.outcome = outcome;
        status.textContent = message;
        status.title = message;
    }
}

function scheduleFrame() {
    if (!started) return;
    if (frameInProgress) { framePending = true; return; }
    if (frameRequest !== 0) return;
    frameRequest = requestAnimationFrame(() => {
        frameRequest = 0;
        frameInProgress = true;
        try {
            plateExports.RenderUiFrame();
        } catch (err) {
            console.error('[Plate-JS] Error in RenderUiFrame:', err);
            showError(err instanceof Error ? err.stack ?? err.message : String(err));
        } finally {
            frameInProgress = false;
            if (framePending) { framePending = false; scheduleFrame(); }
        }
    });
}

function addListener(target, type, listener, options) {
    target.addEventListener(type, listener, options);
    listenerCleanups.push(() => target.removeEventListener(type, listener, options));
}

function localPoint(event) {
    const rect = canvas.getBoundingClientRect();
    return {
        x: (event.clientX - rect.left) * observedWidth / Math.max(1, rect.width),
        y: (event.clientY - rect.top) * observedHeight / Math.max(1, rect.height),
    };
}

function keyboardModifiers(event) {
    return (event.shiftKey ? 1 : 0) |
        (event.ctrlKey || event.metaKey ? 2 : 0) |
        (event.altKey ? 4 : 0) |
        (event.metaKey ? 8 : 0);
}

function canonicalKey(key) {
    if (key === ' ') return 'Space';
    if (key === 'Esc') return 'Escape';
    if (key.startsWith('Arrow')) return key.slice(5);
    return key || 'Unknown';
}

function dispatchKey(event, down) {
    const key = canonicalKey(event.key);
    if ((event.ctrlKey || event.metaKey) && ["c", "x", "v"].includes(key.toLowerCase())) {
        return;
    }
    if (nativeCompositionActive && (event.isComposing || key === 'Process')) return;
    if ((event.ctrlKey || event.metaKey) && EDITOR_CTRL_KEYS.has(key.toLowerCase())) {
        event.preventDefault();
    } else if (!event.ctrlKey && !event.metaKey &&
        (key === 'Space' ? event.target !== textInput :
            ['Tab', 'Up', 'Down', 'Left', 'Right', 'Home', 'End', 'Backspace', 'Delete', 'Enter', 'Escape', 'PageUp', 'PageDown'].includes(key))) {
        event.preventDefault();
    }
    plateExports.UiKeyboardKey(
        key, down, keyboardModifiers(event), event.keyCode || event.which || 0,
        Boolean(event.repeat), Number(event.location || 0), event.timeStamp);
}

async function loadFile(file) {
    if (!file) {
        fileBusy = false;
        return;
    }
    fileBusy = true;
    try {
        reportOperation('Loading', `Reading ${file.name}…`);
        const buffer = await file.arrayBuffer();
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (let offset = 0; offset < bytes.length; offset += 0x8000) {
            binary += String.fromCharCode(...bytes.subarray(offset, offset + 0x8000));
        }
        plateExports.LoadDocument(file.name, btoa(binary));
    } catch (error) {
        reportOperation('Failed', `Error reading file ${file.name}: ${error}`);
    } finally {
        fileBusy = false;
        canvas?.focus({ preventScroll: true });
    }
}

function requestOpenFile(accept) {
    if (!filePicker || fileBusy) return;
    fileBusy = true;
    filePicker.value = '';
    filePicker.accept = accept || '.pdf,.rtf,.docx,.html,.htm,.md,.png,.jpg,.jpeg,.bmp';
    try {
        filePicker.click();
    } catch (error) {
        fileBusy = false;
        reportOperation('Failed', String(error));
    }
}

function requestOpenFolder() {
    if (!folderPicker || fileBusy) return;
    fileBusy = true;
    folderPicker.value = '';
    try {
        folderPicker.click();
    } catch (error) {
        fileBusy = false;
        reportOperation('Failed', String(error));
    }
}

function attachInput() {
    canvas = document.getElementById('plate-canvas');
    host = document.getElementById('plate-host');
    textInput = document.getElementById('plate-text-input');
    statusLine = document.getElementById('plate-status');
    setupTextInput();

    filePicker = document.createElement('input');
    filePicker.type = 'file';
    filePicker.style.display = 'none';
    document.body.appendChild(filePicker);
    addListener(filePicker, 'change', async () => {
        const file = filePicker.files?.[0];
        filePicker.value = '';
        if (file) {
            await loadFile(file);
        } else {
            fileBusy = false;
        }
    });
    addListener(filePicker, 'cancel', () => {
        fileBusy = false;
        reportOperation('Ready', 'Open cancelled.');
        canvas?.focus({ preventScroll: true });
    });

    folderPicker = document.createElement('input');
    folderPicker.type = 'file';
    folderPicker.webkitdirectory = true;
    folderPicker.style.display = 'none';
    document.body.appendChild(folderPicker);
    addListener(folderPicker, 'change', async () => {
        try {
            const files = Array.from(folderPicker.files || []);
            folderPicker.value = '';
            if (!files.length) { fileBusy = false; return; }
            const folderName = files[0].webkitRelativePath?.split('/')[0] || 'Gallery';
            reportOperation('Loading', `Loading folder ${folderName}…`);
            const fileNames = [];
            const base64Datas = [];
            for (const file of files) {
                const buffer = await file.arrayBuffer();
                const bytes = new Uint8Array(buffer);
                let binary = '';
                for (let offset = 0; offset < bytes.length; offset += 0x8000) {
                    binary += String.fromCharCode(...bytes.subarray(offset, offset + 0x8000));
                }
                fileNames.push(file.name);
                base64Datas.push(btoa(binary));
            }
            plateExports.LoadFolder(folderName, fileNames, base64Datas);
        } catch (error) {
            reportOperation('Failed', 'Open folder failed: ' + error);
        } finally {
            fileBusy = false;
            canvas?.focus({ preventScroll: true });
        }
    });
    addListener(folderPicker, 'cancel', () => {
        fileBusy = false;
        reportOperation('Ready', 'Open folder cancelled.');
        canvas?.focus({ preventScroll: true });
    });

    for (const target of [canvas, textInput])
        addListener(target, 'contextmenu', event => event.preventDefault());

    addListener(canvas, 'pointerdown', event => {
        try { canvas.setPointerCapture?.(event.pointerId); } catch { }
        const { x, y } = localPoint(event);
        plateExports.UiPointerButton(x, y, event.buttons, event.button, true, event.timeStamp);
        canvas.focus({ preventScroll: true });
    });

    addListener(canvas, 'pointerup', event => {
        const { x, y } = localPoint(event);
        plateExports.UiPointerButton(x, y, event.buttons, event.button, false, event.timeStamp);
    });

    for (const type of ['pointercancel', 'lostpointercapture'])
        addListener(canvas, type, event => plateExports.UiCancelPointer(event.timeStamp));
    addListener(window, 'blur', event => {
        plateExports.UiCancelPointer(event.timeStamp);
        if (nativeCompositionActive) {
            nativeCompositionActive = false;
            plateExports.UiTextComposition('', 3, event.timeStamp);
        }
    });
    addListener(window, 'focus', () => {
        setTimeout(() => {
            if (fileBusy && (!filePicker?.files || filePicker.files.length === 0) &&
                (!folderPicker?.files || folderPicker.files.length === 0)) {
                const status = document.getElementById('plate-operation');
                if (status?.dataset?.outcome !== 'Loading') {
                    fileBusy = false;
                }
            }
        }, 300);
    });

    addListener(canvas, 'pointermove', event => {
        const { x, y } = localPoint(event);
        plateExports.UiPointerMove(x, y, event.buttons, event.timeStamp);
    });

    addListener(canvas, 'wheel', event => {
        event.preventDefault();
        const { x, y } = localPoint(event);
        const horizontal = Math.abs(event.deltaX) > Math.abs(event.deltaY);
        const delta = horizontal ? event.deltaX : event.deltaY;
        const unit = event.deltaMode === 0 ? 100 : event.deltaMode === 1 ? 3 : 1;
        const notches = -delta / unit;
        plateExports.UiPointerWheel(x, y, event.buttons, horizontal, notches, event.timeStamp);
    }, { passive: false });

    addListener(canvas, 'keydown', event => dispatchKey(event, true));
    addListener(canvas, 'keyup', event => dispatchKey(event, false));

    addListener(canvas, 'dragover', event => {
        event.preventDefault();
        if (event.dataTransfer) event.dataTransfer.dropEffect = 'copy';
    });
    addListener(canvas, 'drop', async event => {
        event.preventDefault();
        const file = event.dataTransfer?.files?.[0];
        if (file) {
            await loadFile(file);
        }
    });

    const resizeObserver = new ResizeObserver(entries => {
        for (const entry of entries) {
            observedWidth = Math.max(1, Math.round(entry.contentRect.width));
            observedHeight = Math.max(1, Math.round(entry.contentRect.height));
            plateExports.ResizeUi(observedWidth, observedHeight, window.devicePixelRatio || 1);
            scheduleFrame();
        }
    });
    resizeObserver.observe(host);
    listenerCleanups.push(() => resizeObserver.disconnect());
}

function setupTextInput() {
    addListener(textInput, 'keydown', event => dispatchKey(event, true));
    addListener(textInput, 'keyup', event => dispatchKey(event, false));
    addListener(textInput, 'beforeinput', event => {
        if (event.isComposing || nativeCompositionActive || (event.inputType || '').includes('Composition')) return;
        if (event.inputType === 'insertText' && event.data) {
            event.preventDefault();
            if (compositionEcho !== null && compositionEcho === event.data) compositionEcho = null;
            else plateExports.UiTextInput(event.data, event.timeStamp);
        } else if ((event.inputType || '').startsWith('delete')) {
            event.preventDefault();
        }
    });
    addListener(textInput, 'input', event => {
        if (compositionEcho !== null && compositionEcho === event.data) { compositionEcho = null; textInput.value = ''; return; }
        if (!nativeCompositionActive && !event.isComposing && event.data)
            plateExports.UiTextInput(event.data, event.timeStamp);
        if (!nativeCompositionActive) textInput.value = '';
    });
    addListener(textInput, 'compositionstart', event => {
        nativeCompositionActive = true;
        plateExports.UiTextComposition('', 0, event.timeStamp);
    });
    addListener(textInput, 'compositionupdate', event => {
        plateExports.UiTextComposition(event.data ?? '', 1, event.timeStamp);
    });
    addListener(textInput, 'compositionend', event => {
        nativeCompositionActive = false;
        compositionEcho = event.data ?? '';
        clearTimeout(compositionEchoTimer);
        compositionEchoTimer = setTimeout(() => { compositionEcho = null; }, 0);
        plateExports.UiTextComposition(event.data ?? '', 2, event.timeStamp);
        textInput.value = '';
    });
    addListener(textInput, 'blur', event => {
        if (!nativeCompositionActive) return;
        nativeCompositionActive = false;
        plateExports.UiTextComposition('', 3, event.timeStamp);
    });
    for (const operation of ['copy', 'cut', 'paste']) {
        for (const target of [textInput, canvas]) addListener(target, operation, event => {
            if (!event.isTrusted) return;
            if (!event.clipboardData) { reportOperation('Failed', 'Browser clipboard data is unavailable.'); return; }
            const incoming = operation === 'paste' ? event.clipboardData?.getData('text/plain') ?? '' : '';
            const outgoing = plateExports.UiClipboardEvent(operation, incoming);
            if (operation === 'paste') {
                event.preventDefault();
            } else {
                if (outgoing) event.clipboardData?.setData('text/plain', outgoing);
            }
            if (operation === 'paste') textInput.value = '';
            else setTimeout(() => { if (!nativeCompositionActive) textInput.value = ''; }, 0);
        });
    }
}

const { setModuleImports, getAssemblyExports, runMain } = await dotnet
    .withEnvironmentVariable('BROILER_IMAGE_DECODE_THREADS', '1')
    .withEnvironmentVariable('BROILER_RASTER_THREADS', '1')
    .create();

setModuleImports('main.js', {
    plate: {
        webMenu: action => {
            if (action === 'home') {
                const link = document.querySelector('.office-nav a');
                if (link) link.click();
                else location.href = 'https://demo.broiler.app/';
            }
        },
        openUrl: url => { window.open(url, '_blank', 'noopener,noreferrer'); },
        scheduleFrame: () => scheduleFrame(),
        setCursor: cursor => { if (canvas) canvas.style.cursor = CURSORS[cursor] || 'default'; },
        setTitle: title => {
            document.title = title;
            const nameEl = document.getElementById('plate-name');
            if (nameEl) nameEl.textContent = title;
        },
        ready: (logicalWidth, logicalHeight) => {
            attachInput();
            started = true;
            observedWidth = Math.max(1, Math.round(host.clientWidth || logicalWidth));
            observedHeight = Math.max(1, Math.round(host.clientHeight || logicalHeight));
            plateExports.ResizeUi(observedWidth, observedHeight, window.devicePixelRatio || 1);
            scheduleFrame();
            if (statusLine) statusLine.hidden = true;
        },
        failed: (exceptionType, details) => showError(`${exceptionType}: ${details}`),
        publishFrame: (frameIndex, active, x, y, width, height, caretIndex, selStart, selLen, statusText, darkTheme) => {
            if (!started || !textInput) return;
            if (statusText) reportOperation('Ready', statusText);
            if (active) {
                textInput.style.left = `${Math.round(x)}px`;
                textInput.style.top = `${Math.round(y)}px`;
                textInput.style.width = `${Math.max(2, Math.round(width))}px`;
                textInput.style.height = `${Math.max(14, Math.round(height))}px`;
            }
        },
        prefersDarkScheme: () => prefersDark(),
        prefersReducedMotion: () => prefersReducedMotion(),
        requestOpenFile: accept => requestOpenFile(accept),
        requestOpenFolder: () => requestOpenFolder(),
        reportOperation: (outcome, message) => reportOperation(outcome, message),
    },
});

const assemblyExports = await getAssemblyExports('Broiler.Plate.WebAssembly');
plateExports = assemblyExports.Broiler.Plate.WebAssembly.BrowserPlateExports;

runMain().catch(error => {
    showError(error instanceof Error ? error.stack ?? error.message : String(error));
});

window.addEventListener('pagehide', () => {
    started = false;
    cancelAnimationFrame(frameRequest);
    for (const cleanup of listenerCleanups.splice(0)) cleanup();
    clearTimeout(compositionEchoTimer);
    filePicker?.remove();
    folderPicker?.remove();
    plateExports.Dispose();
}, { once: true });
window.addEventListener('pageshow', event => { if (event.persisted) location.reload(); });
