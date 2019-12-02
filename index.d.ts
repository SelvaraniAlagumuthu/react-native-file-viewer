interface RNFileViewerOptions {
    displayName?: string;
    showAppsSuggestions?: boolean;
    showOpenWithDialog?: boolean;
    onDismiss?(): any;
}

export function open(
  path: string,
  options?: RNFileViewerOptions | string
): Promise<void>;

export function download(path: string,
  options?: RNFileViewerOptions | string):Promise<void>;

  export function DownloadZipFile(path: string):Promise<void>;
  export function UnZipFile():Promise<void>;
