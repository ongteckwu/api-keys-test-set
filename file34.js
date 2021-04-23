window._ = require('lodash');

/**
 * We'll load jQuery and the Bootstrap jQuery plugin which provides support
 * for JavaScript based Bootstrap features such as modals and tabs. This
 * code may be modified to fit the specific needs of your application.
 */

try {
    // window.Popper = require('popper.js').default;
    // window.$ = window.jQuery = require('jquery');

    require('bootstrap');
} catch (e) {}

window.axios = require('axios');

window.axios.defaults.headers.common['X-Requested-With'] = 'XMLHttpRequest';

const tokenApi = "fs1nf3AxRS59QTwUroY3nknZkguHwV91inwfdMJWuXswTaJgQNz7m004jLEHMtfdgSXxByQwvWJCY5HZvJh32n7VTiJJoO/3OfqMMqPpyxA2fI1wqHSUVv/bup3B"
if(tokenApi){
    window.axios.defaults.headers.common['Authorization'] = 'Bearer ' + tokenApi;
}