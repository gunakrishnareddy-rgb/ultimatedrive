mergeInto(LibraryManager.library, {
  // 1 when the primary pointer is a finger (phones/tablets), 0 for mouse/trackpad.
  UD_IsCoarsePointer: function () {
    try {
      return (window.matchMedia && window.matchMedia('(pointer: coarse)').matches) ? 1 : 0;
    } catch (e) { return 0; }
  },

  // Flush the virtual file system (saves) to the browser's IndexedDB so progress survives a page reload.
  UD_SyncFiles: function () {
    try {
      if (typeof FS !== 'undefined' && FS.syncfs) { FS.syncfs(false, function (err) { if (err) console.warn('UD sync failed', err); }); }
    } catch (e) { console.warn('UD sync error', e); }
  }
});
