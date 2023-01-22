import matplotlib.pyplot as plt
import random
import country as c

# Geojson download: https://datahub.io/core/geo-countries#data-cli

content = None

country_range = 1
chosen_countries = []

countries_to_plot = [
    'Saudi Arabia'
]

for i in range(100):

    ax = plt.figure().add_subplot(projection='3d')
    random_country = c.countries[random.randint(0, len(c.fullGeometryData.keys()) - 1)]

    geometry_x, geometry_y, ispolygon = c.get_country_geometry(random_country)
    if ispolygon:
        geometry_x = c.get_country_geometry(random_country)

    total_x = []
    total_y = []
    total_z = []

    if not ispolygon:
        for j in range(len(geometry_x)):
            coords = c.point_to_sphere(geometry_y[j], geometry_x[j])
            total_x.append(coords[0])
            total_z.append(coords[1])
            total_y.append(coords[2])

        ax.plot(total_x, total_y, total_z, color='black')

    else:
        geometry_x = geometry_x[0]

        for j in range(len(geometry_x)):
            t = len(geometry_x[j])

            total_x = []
            total_y = []
            total_z = []

            for count in range(int(t / 2)):
                lat = geometry_x[j][count * 2]
                lon = geometry_x[j][count * 2 + 1]
                coords = c.point_to_sphere(lat, lon)

                total_x.append(coords[0])
                total_z.append(coords[1])
                total_y.append(coords[2])

    ax.plot(total_x, total_y, total_z, color='black', label=random_country)
    plt.legend(loc="upper left")
    plt.show()
