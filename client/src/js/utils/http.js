import _ from 'lodash';

import * as Action from '../actionTypes';
import store from '../store';
import { keycloak } from '../Keycloak';

import * as Constant from '../constants';

import { resetSessionTimeoutTimer } from '../App.jsx';

const ROOT_API_PREFIX =
  window.location.pathname === '/' ? '' : window.location.pathname.split('/').slice(0, -1).join('/');

var numRequestsInFlight = 0;

function incrementRequests() {
  numRequestsInFlight += 1;
  if (numRequestsInFlight === 1) {
    store.dispatch({ type: Action.REQUESTS_BEGIN });
  }
}

function decrementRequests() {
  numRequestsInFlight -= 1;
  if (numRequestsInFlight <= 0) {
    numRequestsInFlight = 0; // sanity check;
    store.dispatch({ type: Action.REQUESTS_END });
  }
}

export const HttpError = function (msg, method, path, status, body) {
  this.message = msg || '';
  this.method = method;
  this.path = path;
  this.status = status || null;
  this.body = body;
};

HttpError.prototype = Object.create(Error.prototype, {
  constructor: { value: HttpError },
});

export const ApiError = function (msg, method, path, status, errorCode, errorDescription, json) {
  this.message = msg || '';
  this.method = method;
  this.path = path;
  this.status = status || null;
  this.errorCode = errorCode || null;
  this.errorDescription = errorDescription || null;
  this.json = json || null;
};

ApiError.prototype = Object.create(Error.prototype, {
  constructor: { value: ApiError },
});

export const Resource404 = function (name, id) {
  this.name = name;
  this.id = id;
};

Resource404.prototype = Object.create(Error.prototype, {
  constructor: { value: Resource404 },
  toString: {
    value() {
      return `Resouce ${this.name} #${this.id} Not Found`;
    },
  },
});

export async function request(path, options) {
  try {
    await keycloak.updateToken(10);
  } catch {
    console.log('Failed to refresh the token, or the session has expired');
  }

  options = options || {};
  options.headers = Object.assign(
    {
      Authorization: `Bearer ${keycloak.token}`,
    },
    options.headers || {}
  );

  var xhr = new XMLHttpRequest();
  var method = (options.method || 'GET').toUpperCase();

  if (!options.files) {
    options.headers = Object.assign(
      {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
      options.headers
    );
  }

  if (options.onUploadProgress) {
    xhr.upload.addEventListener('progress', function (e) {
      if (e.lengthComputable) {
        options.onUploadProgress((e.loaded / e.total) * 100);
      } else {
        options.onUploadProgress(null);
      }
    });

    xhr.upload.addEventListener('load', function (/*e*/) {
      options.onUploadProgress(100);
    });
  }

  if (options.responseType && window.navigator.appName !== 'Netscape') {
    xhr.responseType = options.responseType;
  } else if (options.responseType && window.navigator.appName === 'Netscape') {
    xhr.open(options.method, path);
    xhr.responseType = options.responseType;
  }

  return new Promise((resolve, reject) => {
    xhr.addEventListener('load', function () {
      if (xhr.status >= 400) {
        var responseText = '';
        try {
          responseText = xhr.responseText;
        } catch (e) {
          /* swallow */
        }

        var err = new HttpError(
          `API ${method} ${path} failed (${xhr.status}) "${responseText}"`,
          method,
          path,
          xhr.status,
          responseText
        );
        reject(err);
      } else {
        // console.log('Call complete! Path: ' + path);
        resolve(xhr);
      }
    });

    xhr.addEventListener('error', function () {
      reject(new HttpError(`Request ${method} ${path} failed to send`, method, path));
    });

    var qs = _.map(options.querystring, (value, key) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`).join(
      '&'
    );

    if (options.responseType) xhr.responseType = options.responseType;

    xhr.open(method, `${path}${qs ? '?' : ''}${qs}`, true);

    Object.keys(options.headers).forEach((key) => {
      xhr.setRequestHeader(key, options.headers[key]);
    });

    if (!options.silent) {
      incrementRequests();
    }

    var payload = options.body || null;

    if (options.files) {
      payload = new FormData();
      if (typeof options.body === 'object') {
        Object.keys(options.body).forEach((key) => {
          payload.append(key, options.body[key]);
        });
      }
      options.files.forEach((file) => {
        payload.append('files', file, file.name);
      });
    }

    xhr.send(payload);
  }).finally(() => {
    if (!options.silent) {
      decrementRequests();
    }
  });
}

export function jsonRequest(path, options) {
  if (!options.keepAlive) {
    resetSessionTimeoutTimer();
  }

  var jsonHeaders = {
    Accept: 'application/json',
  };

  if (options.body) {
    options.body = JSON.stringify(options.body);
    jsonHeaders['Content-Type'] = 'application/json';
  }

  options.headers = Object.assign(options.headers || {}, jsonHeaders);

  return request(path, options)
    .then((xhr) => {
      if (xhr.status === 204) {
        return;
      } else if (xhr.responseType === Constant.RESPONSE_TYPE_BLOB) {
        return xhr.response;
      } else if (options.ignoreResponse) {
        return null;
      } else {
        return xhr.responseText ? JSON.parse(xhr.responseText) : null;
      }
    })
    .catch((err) => {
      if (err instanceof HttpError) {
        var errMsg = `API ${err.method} ${err.path} failed (${err.status})`;
        var json = null;
        var errorCode = null;
        var errorDescription = null;
        try {
          // Example error payload from server:
          // {
          //   "responseStatus": "ERROR",
          //   "data": null,
          //   "error": {
          //     "error": "HETS-01",
          //     "description": "Record not found"
          //   }
          // }

          json = JSON.parse(err.body);
          errorCode = json.error.error;
          errorDescription = json.error.description;
        } catch (err) {
          /* not json */
        }

        throw new ApiError(errMsg, err.method, err.path, err.status, errorCode, errorDescription, json);
      } else {
        throw err;
      }
    });
}

export function buildApiPath(path) {
  return `/api${path}`.replace('//', '/'); // remove double slashes
}

export function ApiRequest(path, options) {
  this.path = `/api${path}`;
  this.options = options;
}

ApiRequest.prototype.get = function apiGet(params, options) {
  return jsonRequest(this.path, {
    method: 'GET',
    querystring: params,
    ...this.options,
    ...options,
  });
};

ApiRequest.prototype.getBlob = function apiGet() {
  return request(this.path, { method: 'GET', responseType: 'blob' });
};

ApiRequest.prototype.post = function apiPost(data, options) {
  return jsonRequest(this.path, {
    method: 'POST',
    body: data,
    ...this.options,
    ...options,
  });
};

ApiRequest.prototype.put = function apiPut(data, options) {
  return jsonRequest(this.path, {
    method: 'PUT',
    body: data,
    ...this.options,
    ...options,
  });
};

ApiRequest.prototype.delete = function apiDelete(data, options) {
  return jsonRequest(this.path, {
    method: 'DELETE',
    body: data,
    ...this.options,
    ...options,
  });
};
