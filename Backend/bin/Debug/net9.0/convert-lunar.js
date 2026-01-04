const { LunarDate } = require('vietnamese-lunar-calendar');

// Get arguments from command line: day month year
const day = parseInt(process.argv[2]);
const month = parseInt(process.argv[3]);
const year = parseInt(process.argv[4]);

try {
    const lunar = new LunarDate(year, month, day);
    // Output as JSON for easy parsing in C#
    console.log(JSON.stringify({
        lunarDay: lunar.date,
        lunarMonth: lunar.month,
        lunarYear: lunar.year,
        leapMonth: 0 // This library doesn't directly expose leap month, we'll need to handle separately
    }));
} catch (error) {
    console.error(JSON.stringify({ error: error.message }));
    process.exit(1);
}
