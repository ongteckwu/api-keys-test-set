package joe.loftus.data;

import java.io.IOException;
import java.net.URL;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Map;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.PropertySource;
import com.fasterxml.jackson.core.JsonGenerationException;
import com.fasterxml.jackson.databind.JsonMappingException;
import com.fasterxml.jackson.databind.ObjectMapper;

import joe.loftus.pojos.SearchResult;
import joe.loftus.pojos.Show;
import joe.loftus.pojos.ShowComparator;

@Configuration
@PropertySource("classpath:application.properties")
public class DataController {
	private double voteThreshold = 100;
	private double popularityThreshold = 10;
	private List<Show> topThree;
	private List<Show> englishTopThree;
	private ObjectMapper mapper = new ObjectMapper();
	private String databaseLocation = "jdbc:sqlite:src/main/java/joe/loftus/greatreleases/shows.db";
	private String moviesEndpoint = "https://api.themoviedb.org/3/movie/now_playing?api_key=""YKu52P8VmzYEzfutUWJh9I";
	private String apiKey = "wScbeoofnLeZia2Kp1ExWWJ1AvdTPzq9rIi2AAUAaDFa7Jnj3Bt8rQNQ3bFYcZoufmsmcaQRhhhMquH0lmLVScKfVw3STfvGK6QmuoR5MwxAeF";

	public DataController() {
		super();
		this.apiKey = System.getenv("apiKey");
	}

	List<Show> returnThreeMovies(List<Show> originalList) {
		Collections.sort(originalList, new ShowComparator());
		return originalList.subList(0, 3);
	}

	public List<Show> getGraphData() {
		topThree = returnThreeMovies(getData());
		return topThree;
	}

	public List<Show> getEnglishGraphData() {
		englishTopThree = returnThreeMovies(getEnglishData());
		return englishTopThree;
	};

	public void putShowsInDatabase() throws SQLException, ClassNotFoundException {
		List<Show> popularShows = new ArrayList<Show>();
		try {
			URL url = new URL(moviesEndpoint + apiKey);
			SearchResult initialResult = this.mapper.readValue(url, SearchResult.class);
			List<Show> initialShows = initialResult.getResults();
			int paginationIndex = Integer.parseInt(initialResult.getTotal_pages());

			// Loop through all shows on first initialResult to prevent call the first api
			// call again
			for (Show show : initialShows) {
				if (Double.valueOf(show.getVote_count()) >= voteThreshold
						&& Double.valueOf(show.getPopularity()) >= popularityThreshold) {
					popularShows.add(show);
				}
			}

			// make an api call to each page except the first and put all qualified shows in
			// the final result
			while (paginationIndex > 1) {
				URL pageUrl = new URL(moviesEndpoint + apiKey + "&page=" + paginationIndex);
				SearchResult pageResult = mapper.readValue(pageUrl, SearchResult.class);
				List<Show> pageShows = pageResult.getResults();
				for (Show pageShow : pageShows) {
					if (Double.valueOf(pageShow.getVote_count()) >= voteThreshold
							&& Double.valueOf(pageShow.getPopularity()) >= popularityThreshold) {
						popularShows.add(pageShow);
					}
				}
				paginationIndex = paginationIndex - 1;
			}

		} catch (JsonGenerationException e) {
			e.printStackTrace();
		} catch (JsonMappingException e) {
			e.printStackTrace();
		} catch (IOException e) {
			e.printStackTrace();
		}
		setData(popularShows);
	}

	private String createGenreIdString(String[] ids) {
		String genreIdString = "";
		for (int i = 0; i < ids.length; i++) {
			genreIdString += ids[i];
			if (i != ids.length - 1) {
				genreIdString += ',';
			}
		}
		return genreIdString;
	}

	public void setData(List<Show> shows) throws SQLException, ClassNotFoundException {
		Connection conn = DriverManager.getConnection(databaseLocation);
		Statement statement = conn.createStatement();
		statement.execute("DROP TABLE IF EXISTS shows");
		statement.execute("CREATE TABLE IF NOT EXISTS shows "
				+ " (popularity TEXT, vote_count TEXT, video TEXT, poster_path TEXT, id TEXT, adult TEXT, backdrop_path TEXT, original_language TEXT, original_title TEXT, genre_ids TEXT[], title TEXT, vote_average TEXT, overview TEXT, release_date TEXT, UNIQUE(id, title));");

		for (int i = 0; i < shows.size(); i++) {
			String string = "INSERT OR IGNORE INTO shows (popularity, vote_count, video, poster_path, id, adult, backdrop_path, original_language, original_title, genre_ids, title, vote_average, overview, release_date) "
					+ "VALUES('" + String.valueOf(shows.get(i).getPopularity()).replaceAll("'", "") + "','"
					+ String.valueOf(shows.get(i).getVote_count()).replaceAll("'", "") + "','"
					+ String.valueOf(shows.get(i).getVideo()).replaceAll("'", "") + "','"
					+ String.valueOf(shows.get(i).getPoster_path()).replaceAll("'", "") + "','"
					+ String.valueOf(shows.get(i).getId()).replaceAll("'", "") + "','"
					+ String.valueOf(shows.get(i).getAdult()).replaceAll("'", "") + "','"
					+ String.valueOf(shows.get(i).getBackdrop_path()).replaceAll("'", "") + "','"
					+ String.valueOf(shows.get(i).getOriginal_language()).replaceAll("'", "") + "','"
					+ String.valueOf(shows.get(i).getOriginal_title()).replaceAll("'", "") + "','("
					+ createGenreIdString(shows.get(i).getGenre_ids()) + ")','"
					+ String.valueOf(shows.get(i).getTitle()).replaceAll("'", "") + "','"
					+ String.valueOf(shows.get(i).getVote_average()).replaceAll("'", "") + "','"
					+ String.valueOf(shows.get(i).getOverview()).replaceAll("'", "") + "','"
					+ String.valueOf(shows.get(i).getRelease_date()).replaceAll("'", "") + "')";

			statement.execute(string);
		}

		statement.close();
		conn.close();
	}
	
	public ArrayList<Show> getData() {
		ArrayList<Show> data = new ArrayList<Show>();
		try {
			Connection conn = DriverManager.getConnection(databaseLocation);
			Statement statement = conn.createStatement();

			statement.execute("SELECT * FROM shows");
			ResultSet results = statement.getResultSet();
			while (results.next()) {
				String popularity = results.getString("popularity");
				String vote_count = results.getString("vote_count");
				String video = results.getString("video");
				String poster_path = results.getString("poster_path");
				String id = results.getString("id");
				String adult = results.getString("adult");
				String backdrop_path = results.getString("backdrop_path");
				String original_language = results.getString("original_language");
				String original_title = results.getString("original_title");
				String genre_ids_array = results.getString("genre_ids");
				String[] genre_ids = new String[] { genre_ids_array };
				String title = results.getString("title");
				String vote_average = results.getString("vote_average");
				String overview = results.getString("overview");
				String release_date = results.getString("release_date");
				Show show = new Show(popularity, vote_count, video, poster_path, id, adult, backdrop_path,
						original_language, original_title, genre_ids, title, vote_average, overview, release_date);
				data.add(show);
			}
			results.close();
			statement.close();
			conn.close();
		} catch (SQLException e) {
			System.out.println("Something went wrong: " + e.getMessage());
		}
		return data;
	}

	public ArrayList<Show> getEnglishData() {
		ArrayList<Show> data = new ArrayList<Show>();
		try {
			Connection conn = DriverManager.getConnection(databaseLocation);
			Statement statement = conn.createStatement();

			statement.execute("SELECT * FROM shows");
			ResultSet results = statement.getResultSet();
			while (results.next()) {
				String original_language = results.getString("original_language");
				if (original_language.equals("en")) {
					String popularity = results.getString("popularity");
					String vote_count = results.getString("vote_count");
					String video = results.getString("video");
					String poster_path = results.getString("poster_path");
					String id = results.getString("id");
					String adult = results.getString("adult");
					String backdrop_path = results.getString("backdrop_path");
					String original_title = results.getString("original_title");
					String genre_ids_array = results.getString("genre_ids");
					String[] genre_ids = new String[] { genre_ids_array };
					String title = results.getString("title");
					String vote_average = results.getString("vote_average");
					String overview = results.getString("overview");
					String release_date = results.getString("release_date");
					Show show = new Show(popularity, vote_count, video, poster_path, id, adult, backdrop_path,
							original_language, original_title, genre_ids, title, vote_average, overview, release_date);
					data.add(show);
				}
			}
			results.close();
			statement.close();
			conn.close();
		} catch (SQLException e) {
			System.out.println("Something went wrong: " + e.getMessage());
		}
		return data;
	}

}
