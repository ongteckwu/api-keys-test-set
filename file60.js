// $.get('/randomWord', function (word) {
//     $.get(`/synonyms/${word}`, function (synonyms) {
//         $.get(`/sentiment/${word}`, function (sentiment) {
//             console.log(`
//             The word ${word} has a 
//             ${sentiment === 1 ? "Positive" : sentiment === -1 ? "Negative" : "Neutral"} sentiment,
//             its synonyms are: ${synonyms}`)
//         })
//     })
// })

// $.get('/randomWord').then //notice that we don't use a callback in this case! We can, but this is what we're avoiding.
// (function(word){
//     console.log(word)
// })
// // console.log(p.state())


// $.get('/sentiment/Ploy').then
// (function(res){
//     console.log(res)
// })

// $.get('/randomWord')
//     .then(function (word) {
//         return $.get(`/synonyms/${word}`)
//     })
//     .then(function (synonyms) {
//         console.log(synonyms)
//     })

//     $.get('/randomWord')
//     .then(function (word) {
//         let synonymsPromise = $.get(`/synonyms/${word}`)
//         let sentimentPromise = $.get(`/sentiment/${word}`)
//         Promise.all([synonymsPromise, sentimentPromise])
//             .then(function (results) {
//                 console.log(results)
//             })
//     })

// const printResults = function (word, synonyms, sentiment) {
//     console.log(`
//         The word ${word} has a 
//         ${sentiment === 1 ? "Positive" : sentiment === -1 ? "Negative" : "Neutral"} sentiment,
//         its synonyms are: ${synonyms}`
//     )
// }

// $.get('/randomWord')
//     .then(function (word) {
//         let synonymsPromise = $.get(`/synonyms/${word}`)
//         let sentimentPromise = $.get(`/sentiment/${word}`)
//         Promise.all([synonymsPromise, sentimentPromise])
//             .then(function (results) {
//                 printResults(word, results[0], results[1])
//             })
//     })

// $.get('/randomWord')
//     .then(function (word) {
//         console.log(word)
//         return $.get(`https://www.googleapis.com/books/v1/volumes?q=title:${word}`)
//             .then(function (book) {
//                 console.log(book)
//                 $("body").append(`<div>${book[0].items[0].volumeInfo.title}</div>`)
//                 return $.get(`http://api.giphy.com/v1/gifs/search?q=${word}&api_key=50m5Set06jQuFMy7VNXir7iaNl8ypsEu`)
//                     .then(function(gifs){
//                         $("body").append(`<iframe src="${gifs[0].data[0].embed_url}" frameborder="0"></iframe>`)
//                     })
//             })
//     })

$.get('/randomWord')
	    .then(function(word) {
	        console.log(word)
	        let randomBook = $.get(`https://www.googleapis.com/books/v1/volumes?q=title:${word}`)
	        let randomGif = $.get(`http://api.giphy.com/v1/gifs/search?q=${word}&api_key=50m5Set06jQuFMy7VNXir7iaNl8ypsEu`)
	        Promise.all([randomBook, randomGif])
	            .then(function(result) {
	                $("body").append(`<div>${result[0].items[0].volumeInfo.title}</div>`)
	                $("body").append(`<iframe src="${result[1].data[0].embed_url}">`)
	            })
	    })

//    Make a request to the /randomWords API
// Then use that word to make a request to the Google Books API
// You should request a book whose title has that random word


// For your convenience, here is the URL for this API: https://www.googleapis.com/books/v1/volumes?q=title:WORD_HERE

// Nice, you've created a random book generator.