(function () {
    'use strict';

    var COLORS = ['#e53935', '#8e24aa', '#1e88e5', '#43a047', '#fb8c00', '#00acc1', '#d81b60', '#6d4c41'];

    function pickColor(userId) {
        var hash = 0;
        var s = String(userId);
        for (var i = 0; i < s.length; i++) {
            hash = ((hash << 5) - hash) + s.charCodeAt(i);
            hash |= 0;
        }
        return COLORS[Math.abs(hash) % COLORS.length];
    }

    function computeTextDelta(oldText, newText) {
        var start = 0;
        var oldLen = oldText.length;
        var newLen = newText.length;

        while (start < oldLen && start < newLen && oldText.charAt(start) === newText.charAt(start)) {
            start++;
        }

        var oldEnd = oldLen;
        var newEnd = newLen;
        while (oldEnd > start && newEnd > start && oldText.charAt(oldEnd - 1) === newText.charAt(newEnd - 1)) {
            oldEnd--;
            newEnd--;
        }

        return {
            index: start,
            removed: oldText.substring(start, oldEnd),
            inserted: newText.substring(start, newEnd)
        };
    }

    function CollaborativeEditor(config) {
        this.documentId = config.documentId;
        this.userId = config.userId;
        this.userName = config.userName;
        this.textarea = config.textarea;
        this.mirror = config.mirror;
        this.cursorsLayer = config.cursorsLayer;
        this.hub = null;
        this.remoteUsers = {};
        this.applyingRemote = false;
        this.lastValue = '';
        this.lastSentIndex = -1;
        this.cursorThrottleTimer = null;
        this.persistTimer = null;
        this.hasLocalEdits = false;
        this.receivedLiveSync = false;
    }

    CollaborativeEditor.prototype.init = function () {
        var self = this;
        if (!window.$ || !$.connection || !$.connection.documentHub) {
            return;
        }

        this.lastValue = this.textarea.value;
        this.hub = $.connection.documentHub;

        this.hub.client.loadDocument = function (content) {
            self.applyFullDocument(content || '');
        };

        this.hub.client.applyTextChange = function (userId, userName, index, removed, inserted, caretIndex) {
            self.applyRemoteChange(userId, userName, index, removed, inserted, caretIndex);
        };

        this.hub.client.updateCursor = function (userId, userName, caretIndex) {
            self.renderRemoteCursor(userId, userName, caretIndex);
        };

        this.hub.client.userLeft = function (userId) {
            self.removeRemoteCursor(userId);
        };

        this.hub.client.userJoined = function (userId, userName) {
            self.hub.server.broadcastDocumentState(self.documentId, self.userId, self.textarea.value);
            self.sendCursorPosition();
        };

        this.hub.client.requestLiveSync = function (joinedUserId) {
            if (String(joinedUserId) !== String(self.userId)) {
                self.hub.server.broadcastDocumentState(self.documentId, self.userId, self.textarea.value);
            }
        };

        this.hub.client.receiveDocumentState = function (fromUserId, content) {
            if (String(fromUserId) === String(self.userId)) {
                return;
            }
            if (!self.receivedLiveSync) {
                self.receivedLiveSync = true;
                self.applyFullDocument(content);
            }
        };

        $.connection.hub.start().done(function () {
            self.hub.server.joinDocument(self.documentId, self.userId, self.userName);
            self.bindEvents();
            setTimeout(function () {
                if (!self.receivedLiveSync) {
                    self.receivedLiveSync = true;
                }
            }, 2000);
        }).fail(function () {
            console.warn('Collaboration connection failed.');
        });

        $(window).on('beforeunload', function () {
            if (self.hasLocalEdits) {
                self.persistDocument(true);
            }
            if (self.hub && $.connection.hub.state === $.signalR.connectionState.connected) {
                self.hub.server.leaveDocument(self.documentId, self.userId);
            }
        });
    };

    CollaborativeEditor.prototype.bindEvents = function () {
        var self = this;

        this.textarea.addEventListener('keydown', function () {
            if (!self.applyingRemote) {
                self.lastValue = self.textarea.value;
            }
        });

        this.textarea.addEventListener('input', function () {
            if (self.applyingRemote) {
                return;
            }

            var newValue = self.textarea.value;
            var delta = computeTextDelta(self.lastValue, newValue);
            self.lastValue = newValue;
            self.hasLocalEdits = true;

            if (delta.removed.length > 0 || delta.inserted.length > 0) {
                self.hub.server.sendTextChange(
                    self.documentId,
                    self.userId,
                    self.userName,
                    delta.index,
                    delta.removed,
                    delta.inserted,
                    self.textarea.selectionStart
                );
            }

            self.scheduleCursorSend();
            self.schedulePersist();
            self.updateLocalMirror();
        });

        ['click', 'keyup', 'select', 'focus'].forEach(function (evt) {
            self.textarea.addEventListener(evt, function () {
                if (!self.applyingRemote) {
                    self.scheduleCursorSend();
                }
            });
        });

        this.textarea.addEventListener('scroll', function () {
            self.repositionAllRemote();
        });

        window.addEventListener('resize', function () {
            self.repositionAllRemote();
        });
    };

    CollaborativeEditor.prototype.applyFullDocument = function (content) {
        this.applyingRemote = true;
        this.textarea.value = content;
        this.lastValue = content;
        this.applyingRemote = false;
        this.updateLocalMirror();
        this.repositionAllRemote();
    };

    CollaborativeEditor.prototype.applyRemoteChange = function (userId, userName, index, removed, inserted, caretIndex) {
        if (String(userId) === String(this.userId)) {
            return;
        }

        this.applyingRemote = true;

        var text = this.textarea.value;
        var safeIndex = Math.max(0, Math.min(index, text.length));
        var removeLen = Math.min(removed.length, text.length - safeIndex);
        var before = text.substring(0, safeIndex);
        var after = text.substring(safeIndex + removeLen);
        this.textarea.value = before + inserted + after;
        this.lastValue = this.textarea.value;

        this.applyingRemote = false;
        this.flashRemoteEdit(userId, userName, safeIndex, inserted.length);
        this.renderRemoteCursor(userId, userName, caretIndex);
        this.updateLocalMirror();
    };

    CollaborativeEditor.prototype.flashRemoteEdit = function (userId, userName, index, insertLength) {
        var entry = this.remoteUsers[userId];
        if (!entry || !entry.marker) {
            return;
        }

        var label = entry.marker.querySelector('.remote-cursor-label');
        if (label) {
            label.textContent = insertLength > 0 ? userName + ' typed' : userName;
        }

        entry.marker.classList.add('remote-cursor-active');
        var self = this;
        clearTimeout(entry.flashTimer);
        entry.flashTimer = setTimeout(function () {
            if (entry.marker) {
                entry.marker.classList.remove('remote-cursor-active');
                var lbl = entry.marker.querySelector('.remote-cursor-label');
                if (lbl) {
                    lbl.textContent = userName;
                }
            }
        }, 1200);
    };

    CollaborativeEditor.prototype.scheduleCursorSend = function () {
        var self = this;
        if (this.cursorThrottleTimer) {
            return;
        }
        this.cursorThrottleTimer = setTimeout(function () {
            self.cursorThrottleTimer = null;
            self.sendCursorPosition();
        }, 40);
    };

    CollaborativeEditor.prototype.sendCursorPosition = function () {
        if (!this.hub || !this.textarea) {
            return;
        }
        var index = this.textarea.selectionStart;
        if (index === this.lastSentIndex) {
            return;
        }
        this.lastSentIndex = index;
        this.hub.server.sendCursor(this.documentId, this.userId, this.userName, index);
    };

    CollaborativeEditor.prototype.schedulePersist = function () {
        var self = this;
        if (this.persistTimer) {
            clearTimeout(this.persistTimer);
        }
        this.persistTimer = setTimeout(function () {
            self.persistDocument();
        }, 1500);
    };

    CollaborativeEditor.prototype.persistDocument = function (sync) {
        if (!this.hub || !this.hasLocalEdits) {
            return;
        }
        this.hub.server.persistContent(this.documentId, this.userId, this.textarea.value);
        this.hasLocalEdits = false;
    };

    CollaborativeEditor.prototype.updateLocalMirror = function () {
        this.syncMirrorStyles();
    };

    CollaborativeEditor.prototype.syncMirrorStyles = function () {
        var style = window.getComputedStyle(this.textarea);
        var props = [
            'boxSizing', 'width', 'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft',
            'borderTopWidth', 'borderRightWidth', 'borderBottomWidth', 'borderLeftWidth',
            'fontStyle', 'fontVariant', 'fontWeight', 'fontStretch', 'fontSize',
            'lineHeight', 'fontFamily', 'textAlign', 'letterSpacing', 'wordSpacing', 'tabSize'
        ];
        for (var i = 0; i < props.length; i++) {
            this.mirror.style[props[i]] = style[props[i]];
        }
        this.mirror.style.width = this.textarea.clientWidth + 'px';
    };

    CollaborativeEditor.prototype.getCaretCoordinates = function (position) {
        this.syncMirrorStyles();
        var text = this.textarea.value.substring(0, position);
        this.mirror.textContent = text.length ? text.replace(/\n$/, '\n\u200b') : '\u200b';

        var marker = document.createElement('span');
        marker.textContent = '\u200b';
        this.mirror.appendChild(marker);

        var markerRect = marker.getBoundingClientRect();
        var mirrorRect = this.mirror.getBoundingClientRect();
        this.mirror.removeChild(marker);

        return {
            top: markerRect.top - mirrorRect.top,
            left: markerRect.left - mirrorRect.left
        };
    };

    CollaborativeEditor.prototype.renderRemoteCursor = function (userId, userName, caretIndex) {
        if (String(userId) === String(this.userId)) {
            return;
        }

        var entry = this.remoteUsers[userId];
        if (!entry) {
            var marker = document.createElement('div');
            marker.className = 'remote-cursor-marker';
            var line = document.createElement('span');
            line.className = 'remote-cursor-line';
            var label = document.createElement('span');
            label.className = 'remote-cursor-label';
            label.textContent = userName;
            var color = pickColor(userId);
            line.style.backgroundColor = color;
            label.style.backgroundColor = color;
            marker.appendChild(line);
            marker.appendChild(label);
            this.cursorsLayer.appendChild(marker);
            entry = { marker: marker, caretIndex: caretIndex, userName: userName };
            this.remoteUsers[userId] = entry;
        }

        entry.caretIndex = caretIndex;
        entry.userName = userName;
        this.positionRemoteMarker(entry);
    };

    CollaborativeEditor.prototype.positionRemoteMarker = function (entry) {
        var len = this.textarea.value.length;
        var idx = Math.max(0, Math.min(entry.caretIndex, len));
        var coords = this.getCaretCoordinates(idx);
        var top = coords.top - this.textarea.scrollTop;
        var left = coords.left - this.textarea.scrollLeft;
        entry.marker.style.transform = 'translate(' + left + 'px, ' + top + 'px)';
    };

    CollaborativeEditor.prototype.repositionAllRemote = function () {
        var self = this;
        Object.keys(this.remoteUsers).forEach(function (userId) {
            self.positionRemoteMarker(self.remoteUsers[userId]);
        });
    };

    CollaborativeEditor.prototype.removeRemoteCursor = function (userId) {
        var entry = this.remoteUsers[userId];
        if (entry) {
            if (entry.flashTimer) {
                clearTimeout(entry.flashTimer);
            }
            entry.marker.parentNode.removeChild(entry.marker);
            delete this.remoteUsers[userId];
        }
    };

    window.initCollaborativeCursors = function () {
        var enabled = document.getElementById('hfCollabEnabled');
        if (!enabled || enabled.value !== 'true') {
            return;
        }

        var docIdField = document.getElementById('hfCurrentDocId');
        var userIdField = document.getElementById('hfCollabUserId');
        var userNameField = document.getElementById('hfCollabUserName');
        var textarea = document.getElementById('docEditor');
        var mirror = document.getElementById('editorMirror');
        var cursorsLayer = document.getElementById('remoteCursors');

        if (!docIdField || !userIdField || !userNameField || !textarea || !mirror || !cursorsLayer) {
            return;
        }

        var documentId = parseInt(docIdField.value, 10);
        if (!documentId || documentId <= 0) {
            return;
        }

        var editor = new CollaborativeEditor({
            documentId: documentId,
            userId: userIdField.value,
            userName: userNameField.value,
            textarea: textarea,
            mirror: mirror,
            cursorsLayer: cursorsLayer
        });
        editor.init();
        window._collaborativeEditor = editor;
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', window.initCollaborativeCursors);
    } else {
        window.initCollaborativeCursors();
    }
})();
